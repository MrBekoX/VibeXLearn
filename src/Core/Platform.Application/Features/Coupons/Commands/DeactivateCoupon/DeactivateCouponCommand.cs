using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Coupons.Commands.DeactivateCoupon;

/// <summary>
/// Command to deactivate a coupon.
/// </summary>
public sealed record DeactivateCouponCommand(Guid CouponId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["coupons:*"];
}
