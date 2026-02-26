using System.Linq.Expressions;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Courses.Queries.GetByInstructorCourse;

/// <summary>
/// Handler for GetByInstructorCourseQuery.
/// </summary>
public sealed class GetByInstructorCourseQueryHandler(
    IReadRepository<Course> readRepo,
    ILogger<GetByInstructorCourseQueryHandler> logger) : IRequestHandler<GetByInstructorCourseQuery, Result<PagedResult<GetByInstructorCourseQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "title", "createdAt", "price", "enrollmentCount", "level", "status" };

    private static readonly Dictionary<string, Expression<Func<Course, object>>> SortFieldMap = new()
    {
        ["title"] = c => c.Title,
        ["createdAt"] = c => c.CreatedAt,
        ["price"] = c => c.Price,
        ["enrollmentCount"] = c => c.EnrollmentCount,
        ["level"] = c => c.Level,
        ["status"] = c => c.Status
    };

    public async Task<Result<PagedResult<GetByInstructorCourseQueryDto>>> Handle(
        GetByInstructorCourseQuery request, CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Query with pagination - filter by instructor
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: c => c.InstructorId == request.InstructorId,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "createdAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [c => c.Category]);

        // Map to DTOs
        var dtos = pagedEntities.Items.Select(c => new GetByInstructorCourseQueryDto
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
            CreatedAt = c.CreatedAt
        }).ToList();

        var result = new PagedResult<GetByInstructorCourseQueryDto>(
            dtos, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);

        logger.LogDebug("Courses by instructor retrieved: {Count} items, InstructorId: {InstructorId}",
            dtos.Count, request.InstructorId);

        return Result.Success(result);
    }
}
