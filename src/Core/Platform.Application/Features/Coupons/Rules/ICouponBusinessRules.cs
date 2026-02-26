using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Coupons.Rules;

/// <summary>
/// Business rules interface for Coupon feature.
/// </summary>
public interface ICouponBusinessRules
{
    IBusinessRule CouponMustExist(Guid couponId);
    IBusinessRule CouponMustExistByCode(string code);
    IBusinessRule CouponCodeMustBeUnique(string code, Guid? excludeCouponId = null);
    IBusinessRule CouponMustBeActive(Guid couponId);
    IBusinessRule CouponMustNotBeExpired(Guid couponId);
    IBusinessRule CouponUsageLimitMustNotBeExceeded(Guid couponId);
    IBusinessRule CouponMustBeValid(string code, decimal orderAmount);
}
