using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Payments.Constants;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Payments.Commands.ProcessCallback;

/// <summary>
/// Handler for ProcessCallbackCommand.
/// Implements 6-step security chain.
/// </summary>
public sealed class ProcessCallbackCommandHandler(
    IReadRepository<PaymentIntent> readRepo,
    IWriteRepository<PaymentIntent> writeRepo,
    IWriteRepository<Order> orderWriteRepo,
    IWriteRepository<Enrollment> enrollmentWriteRepo,
    IIyzicoService iyzicoService,
    IUnitOfWork uow,
    ILogger<ProcessCallbackCommandHandler> logger) : IRequestHandler<ProcessCallbackCommand, Result>
{
    public async Task<Result> Handle(ProcessCallbackCommand request, CancellationToken ct)
    {
        // ADIM 1: ConversationId whitelist — DB'de var mı?
        var paymentIntent = await readRepo.GetAsync(
            p => p.ConversationId == request.ConversationId, ct, tracking: true,
            p => p.Order);

        if (paymentIntent is null)
        {
            logger.LogWarning("{Event} | Unknown ConversationId: {ConversationId}",
                "PAYMENT_CALLBACK_UNKNOWN", request.ConversationId);
            // Always return 200 to prevent retry storm
            return Result.Success();
        }

        // ADIM 2: Idempotency — zaten işlendi mi?
        if (paymentIntent.Status == PaymentStatus.Completed)
        {
            logger.LogInformation("Duplicate callback ignored. ConversationId: {ConversationId}",
                request.ConversationId);
            return Result.Success();
        }

        if (paymentIntent.Status == PaymentStatus.Failed)
        {
            logger.LogWarning("Callback for already failed payment. ConversationId: {ConversationId}",
                request.ConversationId);
            return Result.Success(); // Still return 200
        }

        // ADIM 3: Raw snapshot kaydet (audit trail)
        paymentIntent.SetRawCallbackSnapshot(request.RawBody);
        await writeRepo.UpdateAsync(paymentIntent, ct);
        await uow.SaveChangesAsync(ct);

        // ADIM 4: Iyzico'dan doğrula — callback body'ye güvenilmez
        RetrieveCheckoutFormResult? retrieved;
        try
        {
            retrieved = await iyzicoService.RetrieveCheckoutFormAsync(
                request.Token, request.ConversationId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Iyzico retrieve failed. ConversationId: {ConversationId}",
                request.ConversationId);
            return Result.Success(); // Return 200, log error
        }

        if (retrieved?.Status != "success")
        {
            paymentIntent.MarkAsFailed($"Iyzico status: {retrieved?.Status ?? "null"}");
            await writeRepo.UpdateAsync(paymentIntent, ct);
            await uow.SaveChangesAsync(ct);

            logger.LogWarning("{Event} | ConversationId: {ConversationId} | Status: {Status}",
                "PAYMENT_FAILED", request.ConversationId, retrieved?.Status);
            return Result.Success();
        }

        // ADIM 5: Price Tampering Attack önlemi — fiyat + currency doğrula
        if (!PriceMatches(retrieved.Price, paymentIntent.ExpectedPrice) ||
            !string.Equals(retrieved.Currency, paymentIntent.Currency, StringComparison.OrdinalIgnoreCase))
        {
            paymentIntent.MarkAsFailed("Price/currency mismatch — possible tampering");
            await writeRepo.UpdateAsync(paymentIntent, ct);
            await uow.SaveChangesAsync(ct);

            logger.LogCritical(
                "{Event} | ConversationId: {ConversationId} | Expected: {Expected}{Currency} | Got: {Got}{GotCurrency}",
                "PAYMENT_PRICE_TAMPERED", request.ConversationId,
                paymentIntent.ExpectedPrice, paymentIntent.Currency,
                retrieved.Price, retrieved.Currency);
            return Result.Success();
        }

        // ADIM 6: Atomik state update — Order Paid + Enrollment aç
        await uow.ExecuteInTransactionAsync(async () =>
        {
            paymentIntent.MarkAsCompleted(retrieved.PaymentId ?? string.Empty);
            paymentIntent.Order.MarkAsPaid();

            await writeRepo.UpdateAsync(paymentIntent, ct);
            await orderWriteRepo.UpdateAsync(paymentIntent.Order, ct);

            // Create enrollment
            var enrollment = Enrollment.Create(paymentIntent.Order.UserId, paymentIntent.Order.CourseId);
            await enrollmentWriteRepo.AddAsync(enrollment, ct);

            await uow.SaveChangesAsync(ct);
        }, ct);

        logger.LogInformation("{Event} | ConversationId: {ConversationId} | OrderId: {OrderId}",
            "PAYMENT_SUCCESS", request.ConversationId, paymentIntent.OrderId);

        return Result.Success();
    }

    private static bool PriceMatches(string? iyzicoPrice, decimal expected)
    {
        if (string.IsNullOrWhiteSpace(iyzicoPrice))
            return false;

        if (!decimal.TryParse(iyzicoPrice, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return false;

        return Math.Abs(parsed - expected) < 0.01m; // 1 cent tolerance
    }
}
