using FluentValidation;
using Platform.Application.Features.Coupons.Commands.CreateCoupon;
using Platform.Application.Features.Coupons.Constants;

namespace Platform.Application.Features.Coupons.Validators;

public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(CouponValidationMessages.CodeRequired)
            .MaximumLength(50).WithMessage(CouponValidationMessages.CodeMaxLength);

        RuleFor(x => x.DiscountAmount)
            .GreaterThan(0).WithMessage(CouponValidationMessages.DiscountAmountPositive);

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0).WithMessage(CouponValidationMessages.UsageLimitPositive);

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage(CouponValidationMessages.ExpiresAtFuture);

        When(x => x.IsPercentage, () =>
        {
            RuleFor(x => x.DiscountAmount)
                .LessThanOrEqualTo(100).WithMessage(CouponValidationMessages.DiscountAmountMax);
        });
    }
}
