using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Orders.Commands.ApplyCoupon;

/// <summary>
/// Command to apply a coupon to an existing order.
/// </summary>
public sealed record ApplyCouponCommand(
    Guid OrderId,
    string CouponCode) : IRequest<Result>;
