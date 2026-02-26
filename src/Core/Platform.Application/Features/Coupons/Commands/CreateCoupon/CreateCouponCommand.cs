using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Coupons.Commands.CreateCoupon;

/// <summary>
/// Command to create a new coupon.
/// </summary>
public sealed record CreateCouponCommand(
    string Code,
    decimal DiscountAmount,
    bool IsPercentage,
    int UsageLimit,
    DateTime ExpiresAt) : IRequest<Result<Guid>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["coupons:*"];
}
