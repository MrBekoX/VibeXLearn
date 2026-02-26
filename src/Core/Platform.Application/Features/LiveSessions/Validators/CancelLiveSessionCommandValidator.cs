using FluentValidation;
using Platform.Application.Features.LiveSessions.Commands.CancelLiveSession;
using Platform.Application.Features.LiveSessions.Constants;

namespace Platform.Application.Features.LiveSessions.Validators;

/// <summary>
/// Validator for CancelLiveSessionCommand.
/// </summary>
public sealed class CancelLiveSessionCommandValidator : AbstractValidator<CancelLiveSessionCommand>
{
    public CancelLiveSessionCommandValidator()
    {
        RuleFor(x => x.LiveSessionId)
            .NotEmpty().WithMessage(LiveSessionValidationMessages.LiveSessionIdEmpty);
    }
}
