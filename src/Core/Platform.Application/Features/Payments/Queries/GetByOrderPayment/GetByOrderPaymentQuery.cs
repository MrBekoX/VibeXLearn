using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Payments.DTOs;

namespace Platform.Application.Features.Payments.Queries.GetByOrderPayment;

/// <summary>
/// Query to get payment intent by order ID.
/// </summary>
public sealed record GetByOrderPaymentQuery(Guid OrderId) : IRequest<Result<GetByOrderPaymentQueryDto>>;
