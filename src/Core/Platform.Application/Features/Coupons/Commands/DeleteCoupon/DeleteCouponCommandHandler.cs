using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Commands.DeleteCoupon;

/// <summary>
/// Handler for DeleteCouponCommand.
/// </summary>
public sealed class DeleteCouponCommandHandler(
    IReadRepository<Coupon> readRepo,
    IWriteRepository<Coupon> writeRepo,
    ICouponBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<DeleteCouponCommandHandler> logger) : IRequestHandler<DeleteCouponCommand, Result>
{
    public async Task<Result> Handle(DeleteCouponCommand request, CancellationToken ct)
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

        // Soft delete
        await writeRepo.SoftDeleteAsync(coupon, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Coupon deleted: {CouponId}", coupon.Id);

        return Result.Success();
    }
}
