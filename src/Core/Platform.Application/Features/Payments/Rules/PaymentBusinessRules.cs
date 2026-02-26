using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Payments.Constants;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Payments.Rules;

/// <summary>
/// Business rules implementation for Payment feature.
/// </summary>
public sealed class PaymentBusinessRules(
    IReadRepository<PaymentIntent> paymentRepo,
    IReadRepository<Order> orderRepo,
    IReadRepository<Course> courseRepo,
    IReadRepository<Enrollment> enrollmentRepo) : IPaymentBusinessRules
{
    public IBusinessRule PaymentIntentMustExist(Guid paymentIntentId)
        => new BusinessRule(
            "PAYMENT_NOT_FOUND",
            PaymentBusinessMessages.NotFoundById,
            async ct =>
            {
                var exists = await paymentRepo.AnyAsync(p => p.Id == paymentIntentId, ct);
                return exists
                    ? Result.Success()
                    : Result.Fail(PaymentBusinessMessages.NotFoundById);
            });

    public IBusinessRule PaymentIntentMustExistByConversationId(string conversationId)
        => new BusinessRule(
            "PAYMENT_CONVERSATION_NOT_FOUND",
            PaymentBusinessMessages.NotFoundByConversationId,
            async ct =>
            {
                var exists = await paymentRepo.AnyAsync(p => p.ConversationId == conversationId, ct);
                return exists
                    ? Result.Success()
                    : Result.Fail(PaymentBusinessMessages.NotFoundByConversationId);
            });

    public IBusinessRule PaymentMustNotBeCompleted(Guid paymentIntentId)
        => new BusinessRule(
            "PAYMENT_ALREADY_COMPLETED",
            PaymentBusinessMessages.AlreadyCompleted,
            async ct =>
            {
                var payment = await paymentRepo.GetByIdAsync(paymentIntentId, ct);
                return payment?.Status != PaymentStatus.Completed
                    ? Result.Success()
                    : Result.Fail(PaymentBusinessMessages.AlreadyCompleted);
            });

    public IBusinessRule PaymentMustNotBeFailed(Guid paymentIntentId)
        => new BusinessRule(
            "PAYMENT_ALREADY_FAILED",
            PaymentBusinessMessages.AlreadyFailed,
            async ct =>
            {
                var payment = await paymentRepo.GetByIdAsync(paymentIntentId, ct);
                return payment?.Status != PaymentStatus.Failed
                    ? Result.Success()
                    : Result.Fail(PaymentBusinessMessages.AlreadyFailed);
            });

    public IBusinessRule PaymentMustBeProcessing(Guid paymentIntentId)
        => new BusinessRule(
            "PAYMENT_NOT_PROCESSING",
            PaymentBusinessMessages.NotProcessing,
            async ct =>
            {
                var payment = await paymentRepo.GetByIdAsync(paymentIntentId, ct);
                return payment?.Status == PaymentStatus.Processing
                    ? Result.Success()
                    : Result.Fail(PaymentBusinessMessages.NotProcessing);
            });

    public IBusinessRule OrderMustBePending(Guid orderId)
        => new BusinessRule(
            "ORDER_NOT_PENDING",
            "Only pending orders can be processed for payment.",
            async ct =>
            {
                var order = await orderRepo.GetByIdAsync(orderId, ct);
                return order?.Status == OrderStatus.Pending
                    ? Result.Success()
                    : Result.Fail("Only pending orders can be processed for payment.");
            });

    public IBusinessRule CourseMustBePublished(Guid courseId)
        => new BusinessRule(
            "COURSE_NOT_PUBLISHED",
            "Course is not available for purchase.",
            async ct =>
            {
                var course = await courseRepo.GetByIdAsync(courseId, ct);
                return course?.Status == CourseStatus.Published
                    ? Result.Success()
                    : Result.Fail("Course is not available for purchase.");
            });

    public IBusinessRule UserMustNotBeEnrolled(Guid userId, Guid courseId)
        => new BusinessRule(
            "USER_ALREADY_ENROLLED",
            "You are already enrolled in this course.",
            async ct =>
            {
                var exists = await enrollmentRepo.AnyAsync(
                    e => e.UserId == userId && e.CourseId == courseId, ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail("You are already enrolled in this course.");
            });

    public IBusinessRule NoPendingPaymentExists(Guid userId, Guid courseId)
        => new BusinessRule(
            "PENDING_PAYMENT_EXISTS",
            "You already have a pending payment for this course.",
            async ct =>
            {
                var exists = await paymentRepo.AnyAsync(
                    p => p.Order.UserId == userId &&
                         p.Order.CourseId == courseId &&
                         p.Status == PaymentStatus.Processing, ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail("You already have a pending payment for this course.");
            });

    public IBusinessRule ConversationIdMustExist(string conversationId)
        => new BusinessRule(
            "INVALID_CONVERSATION_ID",
            PaymentBusinessMessages.InvalidCallback,
            async ct =>
            {
                var exists = await paymentRepo.AnyAsync(p => p.ConversationId == conversationId, ct);
                return exists
                    ? Result.Success()
                    : Result.Fail(PaymentBusinessMessages.InvalidCallback);
            });

    public IBusinessRule PriceMustMatch(decimal expected, decimal actual, string currency)
        => new BusinessRule(
            "PRICE_MISMATCH",
            PaymentBusinessMessages.PriceTampered,
            async ct =>
            {
                var tolerance = 0.01m; // 1 cent tolerance
                var matches = Math.Abs(expected - actual) < tolerance;
                return matches
                    ? Result.Success()
                    : Result.Fail(PaymentBusinessMessages.PriceTampered);
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
