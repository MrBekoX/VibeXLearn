using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Payments.Constants;
using Platform.Application.Features.Payments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Payments.Queries.GetByOrderPayment;

/// <summary>
/// Handler for GetByOrderPaymentQuery.
/// </summary>
public sealed class GetByOrderPaymentQueryHandler(
    IReadRepository<PaymentIntent> readRepo) : IRequestHandler<GetByOrderPaymentQuery, Result<GetByOrderPaymentQueryDto>>
{
    public async Task<Result<GetByOrderPaymentQueryDto>> Handle(
        GetByOrderPaymentQuery request, CancellationToken ct)
    {

        // Get payment intent by order
        var payment = await readRepo.GetAsync(
            p => p.OrderId == request.OrderId, ct);

        if (payment is null)
            return Result.Fail<GetByOrderPaymentQueryDto>("PAYMENT_NOT_FOUND", PaymentBusinessMessages.NotFound);

        // Map to DTO
        var dto = new GetByOrderPaymentQueryDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            ConversationId = payment.ConversationId,
            Status = payment.Status.ToString(),
            ExpectedPrice = payment.ExpectedPrice,
            FailReason = payment.FailReason,
            CreatedAt = payment.CreatedAt
        };

        return Result.Success(dto);
    }
}
