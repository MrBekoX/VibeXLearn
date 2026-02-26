using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Payments.Constants;
using Platform.Application.Features.Payments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Payments.Queries.GetByIdPayment;

/// <summary>
/// Handler for GetByIdPaymentQuery.
/// </summary>
public sealed class GetByIdPaymentQueryHandler(
    IReadRepository<PaymentIntent> readRepo) : IRequestHandler<GetByIdPaymentQuery, Result<GetByIdPaymentQueryDto>>
{
    public async Task<Result<GetByIdPaymentQueryDto>> Handle(
        GetByIdPaymentQuery request, CancellationToken ct)
    {

        // Get payment intent
        var payment = await readRepo.GetByIdAsync(request.PaymentIntentId, ct);
        if (payment is null)
            return Result.Fail<GetByIdPaymentQueryDto>("PAYMENT_NOT_FOUND", PaymentBusinessMessages.NotFoundById);

        // Map to DTO
        var dto = new GetByIdPaymentQueryDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            ConversationId = payment.ConversationId,
            IyzicoToken = payment.IyzicoToken,
            IyzicoPaymentId = payment.IyzicoPaymentId,
            ExpectedPrice = payment.ExpectedPrice,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            FailReason = payment.FailReason,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };

        return Result.Success(dto);
    }
}
