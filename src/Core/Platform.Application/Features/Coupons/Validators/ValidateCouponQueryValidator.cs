using FluentValidation;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.Queries.ValidateCoupon;

namespace Platform.Application.Features.Coupons.Validators;

public sealed class ValidateCouponQueryValidator : AbstractValidator<ValidateCouponQuery>
{
    public ValidateCouponQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(CouponValidationMessages.CodeRequired)
            .MaximumLength(50).WithMessage(CouponValidationMessages.CodeMaxLength);

        RuleFor(x => x.OrderAmount)
            .GreaterThan(0).WithMessage(CouponValidationMessages.DiscountAmountPositive);
    }
}
