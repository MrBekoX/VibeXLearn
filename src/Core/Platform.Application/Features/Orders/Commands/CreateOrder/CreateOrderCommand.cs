using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Command to create a new order.
/// </summary>
public sealed record CreateOrderCommand(
    Guid UserId,
    Guid CourseId,
    string? CouponCode = null) : IRequest<Result<Guid>>;
