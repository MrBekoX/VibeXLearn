namespace Platform.Application.Features.Payments.Constants;

/// <summary>
/// Business messages for Payment feature.
/// </summary>
public static class PaymentBusinessMessages
{
    public const string NotFound = "Payment intent not found.";
    public const string NotFoundById = "Payment intent not found with the specified ID.";
    public const string NotFoundByConversationId = "Payment intent not found with the specified conversation ID.";
    public const string AlreadyCompleted = "Payment has already been completed.";
    public const string AlreadyFailed = "Payment has already failed.";
    public const string AlreadyCancelled = "Payment has been cancelled.";
    public const string NotProcessing = "Payment is not in processing state.";
    public const string ProviderUnavailable = "Payment provider is temporarily unavailable. Please try again.";
    public const string PaymentFailed = "Payment could not be completed. Please try again.";
    public const string PriceTampered = "Payment amount mismatch detected. Transaction rejected.";
    public const string InvalidCallback = "Invalid payment callback received.";
    public const string VerificationFailed = "Payment verification failed.";
}
