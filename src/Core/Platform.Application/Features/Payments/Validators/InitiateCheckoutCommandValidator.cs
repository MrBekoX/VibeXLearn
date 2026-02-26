using FluentValidation;
using Platform.Application.Features.Payments.Commands.InitiateCheckout;
using Platform.Application.Features.Payments.Constants;

namespace Platform.Application.Features.Payments.Validators;

public sealed class InitiateCheckoutCommandValidator : AbstractValidator<InitiateCheckoutCommand>
{
    public InitiateCheckoutCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(PaymentValidationMessages.UserIdRequired);

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(PaymentValidationMessages.CourseIdRequired);

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage(PaymentValidationMessages.CouponCodeMaxLength)
            .When(x => x.CouponCode is not null);
    }
}
