using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Commands.ApplyCoupon;

/// <summary>
/// Handler for ApplyCouponCommand.
/// </summary>
public sealed class ApplyCouponCommandHandler(
    IReadRepository<Order> readRepo,
    IReadRepository<Coupon> couponRepo,
    IWriteRepository<Order> writeRepo,
    IOrderBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<ApplyCouponCommandHandler> logger) : IRequestHandler<ApplyCouponCommand, Result>
{
    public async Task<Result> Handle(ApplyCouponCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.OrderMustExist(request.OrderId),
            rules.OrderMustBeCreated(request.OrderId),
            rules.CouponMustBeValid(request.CouponCode),
            rules.CouponMustBeActive(request.CouponCode),
            rules.CouponMustNotBeExpired(request.CouponCode),
            rules.CouponUsageLimitMustNotBeExceeded(request.CouponCode));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get order with tracking
        var order = await readRepo.GetByIdAsync(request.OrderId, ct, tracking: true);
        if (order is null)
            return Result.Fail("ORDER_NOT_FOUND", OrderBusinessMessages.NotFoundById);

        // Get coupon
        var normalized = request.CouponCode.ToUpperInvariant();
        var coupon = await couponRepo.GetAsync(c => c.Code.ToUpper() == normalized, ct);
        if (coupon is null)
            return Result.Fail("COUPON_NOT_FOUND", OrderBusinessMessages.CouponNotFound);

        // Apply coupon using domain method
        order.ApplyCoupon(coupon);

        await writeRepo.UpdateAsync(order, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Coupon applied to order: {OrderId}, Coupon: {CouponCode}",
            order.Id, request.CouponCode);

        return Result.Success();
    }
}
