namespace Platform.Application.Features.Coupons.Constants;

/// <summary>
/// Validation messages for Coupon feature.
/// </summary>
public static class CouponValidationMessages
{
    public const string CouponIdRequired = "Coupon ID is required.";
    public const string CodeRequired = "Coupon code is required.";
    public const string CodeMaxLength = "Coupon code cannot exceed 50 characters.";
    public const string DiscountAmountPositive = "Discount amount must be greater than zero.";
    public const string DiscountAmountMax = "Discount amount cannot exceed 99,999.99.";
    public const string UsageLimitPositive = "Usage limit must be at least 1.";
    public const string UsageLimitMax = "Usage limit cannot exceed 1,000,000.";
    public const string ExpiresAtRequired = "Expiration date is required.";
    public const string ExpiresAtFuture = "Expiration date must be in the future.";
}
