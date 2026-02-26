using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.DTOs;

namespace Platform.Application.Features.Coupons.Queries.GetByIdCoupon;

/// <summary>
/// Query to get coupon by ID.
/// </summary>
public sealed record GetByIdCouponQuery(Guid CouponId)
    : IRequest<Result<GetByIdCouponQueryDto>>, ICacheableQuery
{
    public string CacheKey => CouponCacheKeys.GetById(CouponId);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
