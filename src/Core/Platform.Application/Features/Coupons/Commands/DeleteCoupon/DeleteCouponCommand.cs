using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Coupons.Commands.DeleteCoupon;

/// <summary>
/// Command to delete a coupon (soft delete).
/// </summary>
public sealed record DeleteCouponCommand(Guid CouponId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["coupons:*"];
}
