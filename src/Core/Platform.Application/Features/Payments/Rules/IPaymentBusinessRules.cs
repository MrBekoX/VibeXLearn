using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Payments.Rules;

/// <summary>
/// Business rules interface for Payment feature.
/// </summary>
public interface IPaymentBusinessRules
{
    IBusinessRule PaymentIntentMustExist(Guid paymentIntentId);
    IBusinessRule PaymentIntentMustExistByConversationId(string conversationId);
    IBusinessRule PaymentMustNotBeCompleted(Guid paymentIntentId);
    IBusinessRule PaymentMustNotBeFailed(Guid paymentIntentId);
    IBusinessRule PaymentMustBeProcessing(Guid paymentIntentId);
    IBusinessRule OrderMustBePending(Guid orderId);
    IBusinessRule CourseMustBePublished(Guid courseId);
    IBusinessRule UserMustNotBeEnrolled(Guid userId, Guid courseId);
    IBusinessRule NoPendingPaymentExists(Guid userId, Guid courseId);
    IBusinessRule ConversationIdMustExist(string conversationId);
    IBusinessRule PriceMustMatch(decimal expected, decimal actual, string currency);
}
