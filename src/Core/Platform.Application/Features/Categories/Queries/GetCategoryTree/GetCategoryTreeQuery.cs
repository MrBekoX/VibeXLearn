using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Categories.Constants;
using Platform.Application.Features.Categories.DTOs;

namespace Platform.Application.Features.Categories.Queries.GetCategoryTree;

public sealed record GetCategoryTreeQuery : IRequest<Result<IList<CategoryTreeDto>>>, ICacheableQuery
{
    public string CacheKey => CategoryCacheKeys.Tree();
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
