namespace Platform.Application.Features.Coupons.Constants;

/// <summary>
/// Business messages for Coupon feature.
/// </summary>
public static class CouponBusinessMessages
{
    public const string NotFound = "Coupon not found.";
    public const string NotFoundById = "Coupon not found with the specified ID.";
    public const string NotFoundByCode = "Coupon not found with the specified code.";
    public const string CodeAlreadyExists = "A coupon with this code already exists.";
    public const string AlreadyInactive = "Coupon is already inactive.";
    public const string AlreadyActive = "Coupon is already active.";
    public const string Expired = "Coupon has expired.";
    public const string UsageLimitExceeded = "Coupon usage limit has been exceeded.";
    public const string NotActive = "Coupon is not active.";
    public const string Invalid = "Coupon is not valid.";
}
