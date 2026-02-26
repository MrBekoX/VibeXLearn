using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Orders.Commands.MarkOrderAsFailed;

/// <summary>
/// Command to mark order as failed (payment failed).
/// </summary>
public sealed record MarkOrderAsFailedCommand(
    Guid OrderId,
    string? Reason = null) : IRequest<Result>;
