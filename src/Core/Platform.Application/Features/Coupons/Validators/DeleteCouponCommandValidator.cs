using FluentValidation;
using Platform.Application.Features.Coupons.Commands.DeleteCoupon;
using Platform.Application.Features.Coupons.Constants;

namespace Platform.Application.Features.Coupons.Validators;

public sealed class DeleteCouponCommandValidator : AbstractValidator<DeleteCouponCommand>
{
    public DeleteCouponCommandValidator()
    {
        RuleFor(x => x.CouponId)
            .NotEmpty().WithMessage(CouponValidationMessages.CouponIdRequired);
    }
}
