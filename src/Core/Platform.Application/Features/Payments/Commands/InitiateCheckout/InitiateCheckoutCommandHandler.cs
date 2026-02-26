using MediatR;
using Platform.Application.Common;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Payments.Constants;
using Platform.Application.Features.Payments.DTOs;
using Platform.Application.Features.Payments.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Payments.Commands.InitiateCheckout;

/// <summary>
/// Handler for InitiateCheckoutCommand.
/// </summary>
public sealed class InitiateCheckoutCommandHandler(
    IReadRepository<Course> courseRepo,
    IReadRepository<Coupon> couponRepo,
    IWriteRepository<Order> orderWriteRepo,
    IWriteRepository<PaymentIntent> paymentWriteRepo,
    IPaymentBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IIyzicoService iyzicoService,
    ICurrentUserService currentUserService,
    IUnitOfWork uow,
    ILogger<InitiateCheckoutCommandHandler> logger) : IRequestHandler<InitiateCheckoutCommand, Result<CheckoutResponseDto>>
{
    public async Task<Result<CheckoutResponseDto>> Handle(
        InitiateCheckoutCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CourseMustBePublished(request.CourseId),
            rules.UserMustNotBeEnrolled(request.UserId, request.CourseId),
            rules.NoPendingPaymentExists(request.UserId, request.CourseId));

        if (ruleResult.IsFailure)
            return Result.Fail<CheckoutResponseDto>(ruleResult.Error);

        // Get course for price
        var course = await courseRepo.GetByIdAsync(request.CourseId, ct);
        if (course is null)
            return Result.Fail<CheckoutResponseDto>("COURSE_NOT_FOUND", "Course not found.");

        // Create order with original course price
        var order = Order.Create(request.UserId, request.CourseId, course.Price);

        // Apply coupon if provided
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var normalized = request.CouponCode.ToUpperInvariant();
            var coupon = await couponRepo.GetAsync(c => c.Code.ToUpper() == normalized, ct);
            if (coupon is not null && coupon.IsValid)
            {
                order.ApplyCoupon(coupon);
            }
        }

        // Generate conversation ID
        var conversationId = ConversationIdGenerator.Generate(request.UserId);

        // Create payment intent with final amount (after coupon)
        var paymentIntent = PaymentIntent.Create(order.Id, conversationId, order.FinalAmount, "TRY");

        await orderWriteRepo.AddAsync(order, ct);
        await paymentWriteRepo.AddAsync(paymentIntent, ct);
        await uow.SaveChangesAsync(ct);

        // Get user info for Iyzico
        var user = currentUserService.GetCurrentUser();

        // Call Iyzico to initialize checkout
        try
        {
            var checkoutResult = await iyzicoService.InitiateCheckoutAsync(
                conversationId, user, course, order.FinalAmount, ct);

            if (checkoutResult.IsFailure)
            {
                // Mark payment as failed
                paymentIntent.MarkAsFailed(checkoutResult.Error.Message);
                await paymentWriteRepo.UpdateAsync(paymentIntent, ct);
                await uow.SaveChangesAsync(ct);

                return Result.Fail<CheckoutResponseDto>(checkoutResult.Error);
            }

            // Update payment with token and mark as processing
            paymentIntent.MarkAsProcessing(checkoutResult.Value.Token);
            order.MarkAsPending();

            await paymentWriteRepo.UpdateAsync(paymentIntent, ct);
            await orderWriteRepo.UpdateAsync(order, ct);
            await uow.SaveChangesAsync(ct);

            logger.LogInformation("Checkout initiated: OrderId={OrderId}, ConversationId={ConversationId}",
                order.Id, conversationId);

            return Result.Success(new CheckoutResponseDto
            {
                CheckoutFormContent = checkoutResult.Value.CheckoutFormContent,
                OrderId = order.Id,
                ConversationId = conversationId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Iyzico checkout failed for OrderId={OrderId}", order.Id);

            paymentIntent.MarkAsFailed("Provider unavailable: " + ex.Message);
            await paymentWriteRepo.UpdateAsync(paymentIntent, ct);
            await uow.SaveChangesAsync(ct);

            return Result.Fail<CheckoutResponseDto>("PROVIDER_UNAVAILABLE", PaymentBusinessMessages.ProviderUnavailable);
        }
    }
}
