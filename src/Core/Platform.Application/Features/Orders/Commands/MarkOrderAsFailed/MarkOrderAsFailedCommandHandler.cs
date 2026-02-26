using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Orders.Constants;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Commands.MarkOrderAsFailed;

/// <summary>
/// Handler for MarkOrderAsFailedCommand.
/// </summary>
public sealed class MarkOrderAsFailedCommandHandler(
    IReadRepository<Order> readRepo,
    IWriteRepository<Order> writeRepo,
    IUnitOfWork uow,
    ILogger<MarkOrderAsFailedCommandHandler> logger) : IRequestHandler<MarkOrderAsFailedCommand, Result>
{
    public async Task<Result> Handle(MarkOrderAsFailedCommand request, CancellationToken ct)
    {
        // Get order with tracking
        var order = await readRepo.GetByIdAsync(request.OrderId, ct, tracking: true);
        if (order is null)
            return Result.Fail("ORDER_NOT_FOUND", OrderBusinessMessages.NotFoundById);

        // Mark as failed using domain method (can be called from any non-paid state)
        order.MarkAsFailed(request.Reason ?? "Payment failed");

        await writeRepo.UpdateAsync(order, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogWarning("Order marked as failed: {OrderId}, Reason: {Reason}",
            order.Id, request.Reason ?? "N/A");

        return Result.Success();
    }
}
