using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Commands.ActivateCoupon;

/// <summary>
/// Handler for ActivateCouponCommand.
/// </summary>
public sealed class ActivateCouponCommandHandler(
    IReadRepository<Coupon> readRepo,
    IWriteRepository<Coupon> writeRepo,
    ICouponBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<ActivateCouponCommandHandler> logger) : IRequestHandler<ActivateCouponCommand, Result>
{
    public async Task<Result> Handle(ActivateCouponCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CouponMustExist(request.CouponId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get coupon with tracking
        var coupon = await readRepo.GetByIdAsync(request.CouponId, ct, tracking: true);
        if (coupon is null)
            return Result.Fail("COUPON_NOT_FOUND", CouponBusinessMessages.NotFoundById);

        if (coupon.IsActive)
            return Result.Fail("COUPON_ALREADY_ACTIVE", CouponBusinessMessages.AlreadyActive);

        // Activate using domain method
        coupon.Activate();

        await writeRepo.UpdateAsync(coupon, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Coupon activated: {CouponId}", coupon.Id);

        return Result.Success();
    }
}
