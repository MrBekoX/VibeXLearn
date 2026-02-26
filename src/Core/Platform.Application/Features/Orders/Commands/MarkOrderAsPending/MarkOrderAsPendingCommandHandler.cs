using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Commands.MarkOrderAsPending;

/// <summary>
/// Handler for MarkOrderAsPendingCommand.
/// </summary>
public sealed class MarkOrderAsPendingCommandHandler(
    IReadRepository<Order> readRepo,
    IWriteRepository<Order> writeRepo,
    IOrderBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<MarkOrderAsPendingCommandHandler> logger) : IRequestHandler<MarkOrderAsPendingCommand, Result>
{
    public async Task<Result> Handle(MarkOrderAsPendingCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.OrderMustExist(request.OrderId),
            rules.OrderMustBeCreated(request.OrderId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get order with tracking
        var order = await readRepo.GetByIdAsync(request.OrderId, ct, tracking: true);
        if (order is null)
            return Result.Fail("ORDER_NOT_FOUND", OrderBusinessMessages.NotFoundById);

        // Mark as pending using domain method
        order.MarkAsPending();

        await writeRepo.UpdateAsync(order, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Order marked as pending: {OrderId}", order.Id);

        return Result.Success();
    }
}
