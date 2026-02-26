using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Queries.GetByCodeCoupon;

/// <summary>
/// Handler for GetByCodeCouponQuery.
/// </summary>
public sealed class GetByCodeCouponQueryHandler(
    IReadRepository<Coupon> readRepo) : IRequestHandler<GetByCodeCouponQuery, Result<GetByIdCouponQueryDto>>
{
    public async Task<Result<GetByIdCouponQueryDto>> Handle(
        GetByCodeCouponQuery request, CancellationToken ct)
    {

        // Get coupon
        var normalized = request.Code.ToUpperInvariant();
        var coupon = await readRepo.GetAsync(c => c.Code.ToUpper() == normalized, ct);
        if (coupon is null)
            return Result.Fail<GetByIdCouponQueryDto>("COUPON_NOT_FOUND", CouponBusinessMessages.NotFoundByCode);

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
