namespace Platform.Application.Features.Orders.Constants;

/// <summary>
/// Business messages for Order feature.
/// </summary>
public static class OrderBusinessMessages
{
    public const string NotFound = "Order not found.";
    public const string NotFoundById = "Order not found with the specified ID.";
    public const string AlreadyPaid = "Order is already paid.";
    public const string AlreadyFailed = "Order has already failed.";
    public const string NotPending = "Only pending orders can be processed.";
    public const string NotCreated = "Only created orders can be pending.";
    public const string CourseNotPurchasable = "Course is not available for purchase.";
    public const string AlreadyEnrolled = "You are already enrolled in this course.";
    public const string PendingOrderExists = "You already have a pending order for this course.";
    public const string CouponNotFound = "Coupon not found.";
    public const string CouponExpired = "Coupon has expired.";
    public const string CouponUsageLimitExceeded = "Coupon usage limit has been exceeded.";
    public const string CouponNotApplicable = "Coupon is not applicable to this order.";
    public const string OrderNotBelongToUser = "Order does not belong to this user.";
}
