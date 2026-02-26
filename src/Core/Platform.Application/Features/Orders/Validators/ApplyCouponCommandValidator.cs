using FluentValidation;
using Platform.Application.Features.Orders.Commands.ApplyCoupon;
using Platform.Application.Features.Orders.Constants;

namespace Platform.Application.Features.Orders.Validators;

public sealed class ApplyCouponCommandValidator : AbstractValidator<ApplyCouponCommand>
{
    public ApplyCouponCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(OrderValidationMessages.OrderIdRequired);

        RuleFor(x => x.CouponCode)
            .NotEmpty().WithMessage(OrderValidationMessages.CouponCodeRequired)
            .MaximumLength(50).WithMessage(OrderValidationMessages.CouponCodeMaxLength);
    }
}
