using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Commands.DeactivateCoupon;

/// <summary>
/// Handler for DeactivateCouponCommand.
/// </summary>
public sealed class DeactivateCouponCommandHandler(
    IReadRepository<Coupon> readRepo,
    IWriteRepository<Coupon> writeRepo,
    ICouponBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<DeactivateCouponCommandHandler> logger) : IRequestHandler<DeactivateCouponCommand, Result>
{
    public async Task<Result> Handle(DeactivateCouponCommand request, CancellationToken ct)
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

        if (!coupon.IsActive)
            return Result.Fail("COUPON_ALREADY_INACTIVE", CouponBusinessMessages.AlreadyInactive);

        // Deactivate using domain method
        coupon.Deactivate();

        await writeRepo.UpdateAsync(coupon, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Coupon deactivated: {CouponId}", coupon.Id);

        return Result.Success();
    }
}
