using System.Linq.Expressions;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Courses.DTOs;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Courses.Queries.GetAllCourses;

/// <summary>
/// Handler for GetAllCoursesQuery.
/// Cache is managed automatically by QueryCachingBehavior — no manual cache calls needed.
/// </summary>
public sealed class GetAllCoursesQueryHandler(
    IReadRepository<Course> readRepo,
    ILogger<GetAllCoursesQueryHandler> logger) : IRequestHandler<GetAllCoursesQuery, Result<PagedResult<GetAllCoursesQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "title", "createdAt", "price", "enrollmentCount", "level" };

    private static readonly Dictionary<string, Expression<Func<Course, object>>> SortFieldMap = new()
    {
        ["title"] = c => c.Title,
        ["createdAt"] = c => c.CreatedAt,
        ["price"] = c => c.Price,
        ["enrollmentCount"] = c => c.EnrollmentCount,
        ["level"] = c => c.Level
    };

    public async Task<Result<PagedResult<GetAllCoursesQueryDto>>> Handle(
        GetAllCoursesQuery request, CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Build search predicate
        Expression<Func<Course, bool>>? searchPredicate = null;
        if (!string.IsNullOrWhiteSpace(pr.Search))
        {
            var term = pr.Search.Trim().ToLower();
            searchPredicate = c => c.Title.ToLower().Contains(term)
                                || (c.Description != null && c.Description.ToLower().Contains(term));
        }

        // Query with pagination
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: searchPredicate,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "createdAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [c => c.Category, c => c.Instructor]);

        // Map to DTOs
        var dtos = pagedEntities.Items.Select(c => new GetAllCoursesQueryDto
        {
            Id = c.Id,
            Title = c.Title,
            Slug = c.Slug,
            ThumbnailUrl = c.ThumbnailUrl,
            Price = c.Price,
            Level = c.Level.ToString(),
            Status = c.Status.ToString(),
            EnrollmentCount = c.EnrollmentCount,
            CategoryName = c.Category?.Name,
            CategoryId = c.CategoryId,
            InstructorName = c.Instructor?.FullName,
            InstructorId = c.InstructorId,
            CreatedAt = c.CreatedAt
        }).ToList();

        var result = new PagedResult<GetAllCoursesQueryDto>(
            dtos, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);

        logger.LogDebug("Courses retrieved from DB: {Count} items, Page {Page}", dtos.Count, pr.Page);

        return Result.Success(result);
    }
}
