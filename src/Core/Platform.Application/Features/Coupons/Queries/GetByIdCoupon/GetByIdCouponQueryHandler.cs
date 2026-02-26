using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Queries.GetByIdCoupon;

/// <summary>
/// Handler for GetByIdCouponQuery.
/// </summary>
public sealed class GetByIdCouponQueryHandler(
    IReadRepository<Coupon> readRepo) : IRequestHandler<GetByIdCouponQuery, Result<GetByIdCouponQueryDto>>
{
    public async Task<Result<GetByIdCouponQueryDto>> Handle(
        GetByIdCouponQuery request, CancellationToken ct)
    {

        // Get coupon
        var coupon = await readRepo.GetByIdAsync(request.CouponId, ct);
        if (coupon is null)
            return Result.Fail<GetByIdCouponQueryDto>("COUPON_NOT_FOUND", CouponBusinessMessages.NotFoundById);

        // Map to DTO
        var dto = new GetByIdCouponQueryDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountAmount = coupon.DiscountAmount,
            IsPercentage = coupon.IsPercentage,
            UsageLimit = coupon.UsageLimit,
            UsedCount = coupon.UsedCount,
            IsActive = coupon.IsActive,
            ExpiresAt = coupon.ExpiresAt,
            CreatedAt = coupon.CreatedAt,
            UpdatedAt = coupon.UpdatedAt
        };

        return Result.Success(dto);
    }
}
