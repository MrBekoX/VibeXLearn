using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Coupons.DTOs;

namespace Platform.Application.Features.Coupons.Queries.ValidateCoupon;

/// <summary>
/// Query to validate a coupon for an order.
/// </summary>
public sealed record ValidateCouponQuery(
    string Code,
    decimal OrderAmount) : IRequest<Result<ValidateCouponQueryDto>>;
