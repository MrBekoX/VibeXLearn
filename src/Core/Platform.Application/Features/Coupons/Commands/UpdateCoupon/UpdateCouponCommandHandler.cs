using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Commands.UpdateCoupon;

/// <summary>
/// Handler for UpdateCouponCommand.
/// </summary>
public sealed class UpdateCouponCommandHandler(
    IReadRepository<Coupon> readRepo,
    IWriteRepository<Coupon> writeRepo,
    ICouponBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<UpdateCouponCommandHandler> logger) : IRequestHandler<UpdateCouponCommand, Result>
{
    public async Task<Result> Handle(UpdateCouponCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CouponMustExist(request.CouponId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Check code uniqueness if changing code
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeResult = await ruleEngine.RunAsync(ct,
                rules.CouponCodeMustBeUnique(request.Code, request.CouponId));
            if (codeResult.IsFailure)
                return codeResult;
        }

        // Get coupon with tracking
        var coupon = await readRepo.GetByIdAsync(request.CouponId, ct, tracking: true);
        if (coupon is null)
            return Result.Fail("COUPON_NOT_FOUND", CouponBusinessMessages.NotFoundById);

        // Update using domain method
        coupon.Update(
            request.Code,
            request.DiscountAmount,
            request.IsPercentage,
            request.UsageLimit,
            request.ExpiresAt);

        await writeRepo.UpdateAsync(coupon, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Coupon updated: {CouponId}", coupon.Id);

        return Result.Success();
    }
}
