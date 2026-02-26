using FluentValidation;
using Platform.Application.Features.Orders.Commands.MarkOrderAsPaid;
using Platform.Application.Features.Orders.Constants;

namespace Platform.Application.Features.Orders.Validators;

public sealed class MarkOrderAsPaidCommandValidator : AbstractValidator<MarkOrderAsPaidCommand>
{
    public MarkOrderAsPaidCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(OrderValidationMessages.OrderIdRequired);
    }
}
