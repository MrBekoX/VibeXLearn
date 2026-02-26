using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.DTOs;

namespace Platform.Application.Features.Coupons.Queries.GetAllCoupons;

/// <summary>
/// Query to get all coupons (paginated).
/// </summary>
public sealed record GetAllCouponsQuery(PageRequest PageRequest)
    : IRequest<Result<PagedResult<GetAllCouponsQueryDto>>>, IPagedQuery, ICacheableQuery
{
    public string CacheKey
    {
        get
        {
            var pr = PageRequest.Normalize();
            return CouponCacheKeys.GetAll(pr.Page, pr.PageSize);
        }
    }
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
