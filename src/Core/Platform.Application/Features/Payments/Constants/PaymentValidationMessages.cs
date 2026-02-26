namespace Platform.Application.Features.Payments.Constants;

/// <summary>
/// Validation messages for Payment feature.
/// </summary>
public static class PaymentValidationMessages
{
    public const string PaymentIntentIdRequired = "Payment intent ID is required.";
    public const string OrderIdRequired = "Order ID is required.";
    public const string TokenRequired = "Payment token is required.";
    public const string ConversationIdRequired = "Conversation ID is required.";
    public const string RawBodyRequired = "Raw body is required.";
    public const string UserIdRequired = "User ID is required.";
    public const string CourseIdRequired = "Course ID is required.";
    public const string CouponCodeMaxLength = "Coupon code cannot exceed 50 characters.";
}
