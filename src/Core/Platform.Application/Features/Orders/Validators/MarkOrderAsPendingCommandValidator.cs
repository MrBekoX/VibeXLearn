using FluentValidation;
using Platform.Application.Features.Orders.Commands.MarkOrderAsPending;
using Platform.Application.Features.Orders.Constants;

namespace Platform.Application.Features.Orders.Validators;

public sealed class MarkOrderAsPendingCommandValidator : AbstractValidator<MarkOrderAsPendingCommand>
{
    public MarkOrderAsPendingCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(OrderValidationMessages.OrderIdRequired);
    }
}
