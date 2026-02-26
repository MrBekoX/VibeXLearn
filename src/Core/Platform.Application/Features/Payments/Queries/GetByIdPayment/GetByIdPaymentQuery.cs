using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Payments.DTOs;

namespace Platform.Application.Features.Payments.Queries.GetByIdPayment;

/// <summary>
/// Query to get payment intent by ID.
/// </summary>
public sealed record GetByIdPaymentQuery(Guid PaymentIntentId) : IRequest<Result<GetByIdPaymentQueryDto>>;
