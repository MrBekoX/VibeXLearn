using FluentValidation;
using Platform.Application.Features.Orders.Commands.CreateOrder;
using Platform.Application.Features.Orders.Constants;

namespace Platform.Application.Features.Orders.Validators;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(OrderValidationMessages.UserIdRequired);

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(OrderValidationMessages.CourseIdRequired);

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage(OrderValidationMessages.CouponCodeMaxLength)
            .When(x => x.CouponCode is not null);
    }
}
