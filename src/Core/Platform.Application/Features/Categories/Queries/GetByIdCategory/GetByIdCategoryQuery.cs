using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Categories.Constants;
using Platform.Application.Features.Categories.DTOs;

namespace Platform.Application.Features.Categories.Queries.GetByIdCategory;

public sealed record GetByIdCategoryQuery(Guid CategoryId)
    : IRequest<Result<GetByIdCategoryQueryDto>>, ICacheableQuery
{
    public string CacheKey => CategoryCacheKeys.GetById(CategoryId);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
