using System.Linq.Expressions;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Categories.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Categories.Queries.GetAllCategories;

/// <summary>
/// Handler for GetAllCategoriesQuery.
/// Cache is managed automatically by QueryCachingBehavior — no manual cache calls needed.
/// </summary>
public sealed class GetAllCategoriesQueryHandler(
    IReadRepository<Category> readRepo,
    ILogger<GetAllCategoriesQueryHandler> logger)
    : IRequestHandler<GetAllCategoriesQuery, Result<PagedResult<GetAllCategoriesQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "name", "createdAt", "slug" };

    private static readonly Dictionary<string, Expression<Func<Category, object>>> SortFieldMap = new()
    {
        ["name"] = c => c.Name,
        ["createdAt"] = c => c.CreatedAt,
        ["slug"] = c => c.Slug
    };

    public async Task<Result<PagedResult<GetAllCategoriesQueryDto>>> Handle(
        GetAllCategoriesQuery request,
        CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Build search predicate (root categories only)
        Expression<Func<Category, bool>>? searchPredicate = null;
        if (!string.IsNullOrWhiteSpace(pr.Search))
        {
            var term = pr.Search.Trim().ToLower();
            searchPredicate = c => c.ParentId == null &&
                (c.Name.ToLower().Contains(term) ||
                 (c.Description != null && c.Description.ToLower().Contains(term)));
        }
        else
        {
            searchPredicate = c => c.ParentId == null;
        }

        // Query with pagination
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: searchPredicate,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "name", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [c => c.Parent!]);

        // Map to DTOs
        var dtos = pagedEntities.Items.Select(c => new GetAllCategoriesQueryDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            ParentId = c.ParentId,
            ParentName = c.Parent?.Name,
            CourseCount = c.Courses?.Count ?? 0
        }).ToList();

        var result = new PagedResult<GetAllCategoriesQueryDto>(
            dtos, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);

        logger.LogDebug("Categories retrieved from DB: {Count} items, Page {Page}", dtos.Count, pr.Page);

        return Result.Success(result);
    }
}
