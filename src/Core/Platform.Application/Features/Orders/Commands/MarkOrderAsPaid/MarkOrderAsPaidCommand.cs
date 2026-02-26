using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Orders.Commands.MarkOrderAsPaid;

/// <summary>
/// Command to mark order as paid (payment completed).
/// </summary>
public sealed record MarkOrderAsPaidCommand(Guid OrderId) : IRequest<Result>;
