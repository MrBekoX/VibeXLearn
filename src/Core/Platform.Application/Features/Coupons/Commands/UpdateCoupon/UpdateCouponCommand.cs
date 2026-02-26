using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Coupons.Commands.UpdateCoupon;

/// <summary>
/// Command to update a coupon.
/// </summary>
public sealed record UpdateCouponCommand(
    Guid CouponId,
    string? Code = null,
    decimal? DiscountAmount = null,
    bool? IsPercentage = null,
    int? UsageLimit = null,
    DateTime? ExpiresAt = null) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["coupons:*"];
}
