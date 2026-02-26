namespace Platform.Application.Features.Orders.Constants;

/// <summary>
/// Validation messages for Order feature.
/// </summary>
public static class OrderValidationMessages
{
    public const string OrderIdRequired = "Order ID is required.";
    public const string UserIdRequired = "User ID is required.";
    public const string CourseIdRequired = "Course ID is required.";
    public const string CouponCodeRequired = "Coupon code is required.";
    public const string CouponCodeMaxLength = "Coupon code cannot exceed 50 characters.";
    public const string AmountPositive = "Order amount must be greater than zero.";
    public const string AmountMax = "Order amount cannot exceed 999,999.99.";
}
