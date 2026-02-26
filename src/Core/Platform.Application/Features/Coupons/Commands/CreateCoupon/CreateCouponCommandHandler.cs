using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Commands.CreateCoupon;

/// <summary>
/// Handler for CreateCouponCommand.
/// </summary>
public sealed class CreateCouponCommandHandler(
    IWriteRepository<Coupon> writeRepo,
    ICouponBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CreateCouponCommandHandler> logger) : IRequestHandler<CreateCouponCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCouponCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CouponCodeMustBeUnique(request.Code));

        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        // Create coupon using domain factory
        var coupon = Coupon.Create(
            request.Code,
            request.DiscountAmount,
            request.IsPercentage,
            request.UsageLimit,
            request.ExpiresAt);

        await writeRepo.AddAsync(coupon, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Coupon created: {CouponId}, Code: {Code}", coupon.Id, coupon.Code);

        return Result.Success(coupon.Id);
    }
}
