using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Badges.Constants;
using Platform.Application.Features.Badges.DTOs;

namespace Platform.Application.Features.Badges.Queries.GetByIdBadge;

/// <summary>
/// Query to get a badge by ID.
/// </summary>
public sealed record GetByIdBadgeQuery(Guid BadgeId)
    : IRequest<Result<GetByIdBadgeQueryDto>>, ICacheableQuery
{
    public string CacheKey => BadgeCacheKeys.GetById(BadgeId);
    public bool BypassCache => false;
}
