using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Orders.Commands.MarkOrderAsPending;

/// <summary>
/// Command to mark order as pending (checkout initiated).
/// </summary>
public sealed record MarkOrderAsPendingCommand(Guid OrderId) : IRequest<Result>;
