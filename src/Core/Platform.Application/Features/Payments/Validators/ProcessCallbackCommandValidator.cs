using FluentValidation;
using Platform.Application.Features.Payments.Commands.ProcessCallback;
using Platform.Application.Features.Payments.Constants;

namespace Platform.Application.Features.Payments.Validators;

public sealed class ProcessCallbackCommandValidator : AbstractValidator<ProcessCallbackCommand>
{
    public ProcessCallbackCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(PaymentValidationMessages.TokenRequired);

        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage(PaymentValidationMessages.ConversationIdRequired);

        RuleFor(x => x.RawBody)
            .NotEmpty().WithMessage(PaymentValidationMessages.RawBodyRequired);
    }
}
