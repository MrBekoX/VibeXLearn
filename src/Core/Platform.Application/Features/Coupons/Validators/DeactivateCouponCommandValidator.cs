using FluentValidation;
using Platform.Application.Features.Coupons.Commands.DeactivateCoupon;
using Platform.Application.Features.Coupons.Constants;

namespace Platform.Application.Features.Coupons.Validators;

public sealed class DeactivateCouponCommandValidator : AbstractValidator<DeactivateCouponCommand>
{
    public DeactivateCouponCommandValidator()
    {
        RuleFor(x => x.CouponId)
            .NotEmpty().WithMessage(CouponValidationMessages.CouponIdRequired);
    }
}
