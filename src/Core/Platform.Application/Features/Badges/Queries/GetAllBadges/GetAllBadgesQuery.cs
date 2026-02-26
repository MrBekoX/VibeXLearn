using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Badges.Constants;
using Platform.Application.Features.Badges.DTOs;

namespace Platform.Application.Features.Badges.Queries.GetAllBadges;

/// <summary>
/// Query to get all badges with pagination.
/// </summary>
public sealed record GetAllBadgesQuery(PageRequest PageRequest)
    : IRequest<Result<PagedResult<GetAllBadgesQueryDto>>>, IPagedQuery, ICacheableQuery
{
    public string CacheKey
    {
        get
        {
            var pr = PageRequest.Normalize();
            return BadgeCacheKeys.GetAll(pr.Page, pr.PageSize);
        }
    }
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
