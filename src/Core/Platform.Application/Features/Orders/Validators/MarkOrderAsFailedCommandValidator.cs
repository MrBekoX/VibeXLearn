using FluentValidation;
using Platform.Application.Features.Orders.Commands.MarkOrderAsFailed;
using Platform.Application.Features.Orders.Constants;

namespace Platform.Application.Features.Orders.Validators;

public sealed class MarkOrderAsFailedCommandValidator : AbstractValidator<MarkOrderAsFailedCommand>
{
    public MarkOrderAsFailedCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(OrderValidationMessages.OrderIdRequired);
    }
}
