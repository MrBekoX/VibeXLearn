using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Categories.Constants;
using Platform.Application.Features.Categories.DTOs;

namespace Platform.Application.Features.Categories.Queries.GetBySlugCategory;

public sealed record GetBySlugCategoryQuery(string Slug)
    : IRequest<Result<GetByIdCategoryQueryDto>>, ICacheableQuery
{
    public string CacheKey => CategoryCacheKeys.BySlug(Slug);
    public bool BypassCache => false;
}
