using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Queries.ValidateCoupon;

/// <summary>
/// Handler for ValidateCouponQuery.
/// </summary>
public sealed class ValidateCouponQueryHandler(
    IReadRepository<Coupon> readRepo,
    ILogger<ValidateCouponQueryHandler> logger) : IRequestHandler<ValidateCouponQuery, Result<ValidateCouponQueryDto>>
{
    public async Task<Result<ValidateCouponQueryDto>> Handle(
        ValidateCouponQuery request, CancellationToken ct)
    {
        // Get coupon
        var normalized = request.Code.ToUpperInvariant();
        var coupon = await readRepo.GetAsync(c => c.Code.ToUpper() == normalized, ct);

        if (coupon is null)
        {
            return Result.Success(new ValidateCouponQueryDto
            {
                Id = Guid.Empty,
                Code = request.Code,
                DiscountAmount = 0,
                IsPercentage = false,
                IsValid = false,
                Message = CouponBusinessMessages.NotFoundByCode
            });
        }

        // Validate coupon
        string? errorMessage = null;
        bool isValid = true;

        if (!coupon.IsActive)
        {
            isValid = false;
            errorMessage = CouponBusinessMessages.NotActive;
        }
        else if (coupon.ExpiresAt <= DateTime.UtcNow)
        {
            isValid = false;
            errorMessage = CouponBusinessMessages.Expired;
        }
        else if (coupon.UsedCount >= coupon.UsageLimit)
        {
            isValid = false;
            errorMessage = CouponBusinessMessages.UsageLimitExceeded;
        }

        // Calculate discount
        decimal discountAmount = 0;
        if (isValid)
        {
            discountAmount = coupon.IsPercentage
                ? request.OrderAmount * (coupon.DiscountAmount / 100)
                : coupon.DiscountAmount;
        }

        logger.LogDebug("Coupon validation: Code={Code}, IsValid={IsValid}", request.Code, isValid);

        return Result.Success(new ValidateCouponQueryDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountAmount = discountAmount,
            IsPercentage = coupon.IsPercentage,
            IsValid = isValid,
            Message = isValid ? null : errorMessage
        });
    }
}
