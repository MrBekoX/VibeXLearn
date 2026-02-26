using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Coupons.Commands.ActivateCoupon;

/// <summary>
/// Command to activate a coupon.
/// </summary>
public sealed record ActivateCouponCommand(Guid CouponId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["coupons:*"];
}
