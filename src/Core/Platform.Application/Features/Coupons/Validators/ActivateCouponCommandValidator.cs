using FluentValidation;
using Platform.Application.Features.Coupons.Commands.ActivateCoupon;
using Platform.Application.Features.Coupons.Constants;

namespace Platform.Application.Features.Coupons.Validators;

public sealed class ActivateCouponCommandValidator : AbstractValidator<ActivateCouponCommand>
{
    public ActivateCouponCommandValidator()
    {
        RuleFor(x => x.CouponId)
            .NotEmpty().WithMessage(CouponValidationMessages.CouponIdRequired);
    }
}
