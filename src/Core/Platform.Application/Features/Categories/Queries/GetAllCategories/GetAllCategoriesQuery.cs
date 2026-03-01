using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Categories.Constants;
using Platform.Application.Features.Categories.DTOs;

namespace Platform.Application.Features.Categories.Queries.GetAllCategories;

/// <summary>
/// Query to get all root categories with pagination.
/// </summary>
public sealed record GetAllCategoriesQuery(PageRequest PageRequest)
    : IRequest<Result<PagedResult<GetAllCategoriesQueryDto>>>, IPagedQuery, ICacheableQuery
{
    public string CacheKey => CategoryCacheKeys.GetAll(PageRequest.Normalize());
    public bool BypassCache => false;
}
