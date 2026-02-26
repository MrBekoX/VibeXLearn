using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Coupons.Constants;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Rules;

/// <summary>
/// Business rules implementation for Coupon feature.
/// </summary>
public sealed class CouponBusinessRules(
    IReadRepository<Coupon> couponRepo) : ICouponBusinessRules
{
    public IBusinessRule CouponMustExist(Guid couponId)
        => new BusinessRule(
            "COUPON_NOT_FOUND",
            CouponBusinessMessages.NotFoundById,
            async ct =>
            {
                var exists = await couponRepo.AnyAsync(c => c.Id == couponId, ct);
                return exists
                    ? Result.Success()
                    : Result.Fail(CouponBusinessMessages.NotFoundById);
            });

    public IBusinessRule CouponMustExistByCode(string code)
        => new BusinessRule(
            "COUPON_NOT_FOUND",
            CouponBusinessMessages.NotFoundByCode,
            async ct =>
            {
                var normalized = code.ToUpperInvariant();
                var exists = await couponRepo.AnyAsync(c => c.Code.ToUpper() == normalized, ct);
                return exists
                    ? Result.Success()
                    : Result.Fail(CouponBusinessMessages.NotFoundByCode);
            });

    public IBusinessRule CouponCodeMustBeUnique(string code, Guid? excludeCouponId = null)
        => new BusinessRule(
            "COUPON_CODE_EXISTS",
            CouponBusinessMessages.CodeAlreadyExists,
            async ct =>
            {
                var normalized = code.ToUpperInvariant();
                var exists = excludeCouponId.HasValue
                    ? await couponRepo.AnyAsync(
                        c => c.Code.ToUpper() == normalized && c.Id != excludeCouponId.Value, ct)
                    : await couponRepo.AnyAsync(c => c.Code.ToUpper() == normalized, ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail(CouponBusinessMessages.CodeAlreadyExists);
            });

    public IBusinessRule CouponMustBeActive(Guid couponId)
        => new BusinessRule(
            "COUPON_NOT_ACTIVE",
            CouponBusinessMessages.NotActive,
            async ct =>
            {
                var coupon = await couponRepo.GetByIdAsync(couponId, ct);
                return coupon?.IsActive == true
                    ? Result.Success()
                    : Result.Fail(CouponBusinessMessages.NotActive);
            });

    public IBusinessRule CouponMustNotBeExpired(Guid couponId)
        => new BusinessRule(
            "COUPON_EXPIRED",
            CouponBusinessMessages.Expired,
            async ct =>
            {
                var coupon = await couponRepo.GetByIdAsync(couponId, ct);
                return coupon is not null && coupon.ExpiresAt > DateTime.UtcNow
                    ? Result.Success()
                    : Result.Fail(CouponBusinessMessages.Expired);
            });

    public IBusinessRule CouponUsageLimitMustNotBeExceeded(Guid couponId)
        => new BusinessRule(
            "COUPON_LIMIT_EXCEEDED",
            CouponBusinessMessages.UsageLimitExceeded,
            async ct =>
            {
                var coupon = await couponRepo.GetByIdAsync(couponId, ct);
                return coupon is not null && coupon.UsedCount < coupon.UsageLimit
                    ? Result.Success()
                    : Result.Fail(CouponBusinessMessages.UsageLimitExceeded);
            });

    public IBusinessRule CouponMustBeValid(string code, decimal orderAmount)
        => new BusinessRule(
            "COUPON_INVALID",
            CouponBusinessMessages.Invalid,
            async ct =>
            {
                var normalized = code.ToUpperInvariant();
                var coupon = await couponRepo.GetAsync(c => c.Code.ToUpper() == normalized, ct);
                if (coupon is null)
                    return Result.Fail(CouponBusinessMessages.NotFoundByCode);
                if (!coupon.IsActive)
                    return Result.Fail(CouponBusinessMessages.NotActive);
                if (coupon.ExpiresAt <= DateTime.UtcNow)
                    return Result.Fail(CouponBusinessMessages.Expired);
                if (coupon.UsedCount >= coupon.UsageLimit)
                    return Result.Fail(CouponBusinessMessages.UsageLimitExceeded);
                return Result.Success();
            });
}

/// <summary>
/// Simple business rule implementation.
/// </summary>
file sealed class BusinessRule(string code, string message, Func<CancellationToken, Task<Result>> checkFunc)
    : IBusinessRule
{
    public string Code => code;
    public string Message => message;
    public Task<Result> CheckAsync(CancellationToken ct) => checkFunc(ct);
}
