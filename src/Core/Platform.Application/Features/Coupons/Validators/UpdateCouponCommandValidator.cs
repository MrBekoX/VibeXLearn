using FluentValidation;
using Platform.Application.Features.Coupons.Commands.UpdateCoupon;
using Platform.Application.Features.Coupons.Constants;

namespace Platform.Application.Features.Coupons.Validators;

public sealed class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    public UpdateCouponCommandValidator()
    {
        RuleFor(x => x.CouponId)
            .NotEmpty().WithMessage(CouponValidationMessages.CouponIdRequired);

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage(CouponValidationMessages.CodeMaxLength)
            .When(x => x.Code is not null);

        RuleFor(x => x.DiscountAmount)
            .GreaterThan(0).WithMessage(CouponValidationMessages.DiscountAmountPositive)
            .When(x => x.DiscountAmount.HasValue);

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0).WithMessage(CouponValidationMessages.UsageLimitPositive)
            .When(x => x.UsageLimit.HasValue);

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage(CouponValidationMessages.ExpiresAtFuture)
            .When(x => x.ExpiresAt.HasValue);
    }
}
