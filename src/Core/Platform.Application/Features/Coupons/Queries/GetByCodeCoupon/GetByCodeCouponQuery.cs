using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.DTOs;

namespace Platform.Application.Features.Coupons.Queries.GetByCodeCoupon;

/// <summary>
/// Query to get coupon by code.
/// </summary>
public sealed record GetByCodeCouponQuery(string Code)
    : IRequest<Result<GetByIdCouponQueryDto>>, ICacheableQuery
{
    public string CacheKey => CouponCacheKeys.ByCode(Code);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
