using FluentValidation;
using Platform.Application.Features.LiveSessions.Commands.StartLiveSession;
using Platform.Application.Features.LiveSessions.Constants;

namespace Platform.Application.Features.LiveSessions.Validators;

/// <summary>
/// Validator for StartLiveSessionCommand.
/// </summary>
public sealed class StartLiveSessionCommandValidator : AbstractValidator<StartLiveSessionCommand>
{
    public StartLiveSessionCommandValidator()
    {
        RuleFor(x => x.LiveSessionId)
            .NotEmpty().WithMessage(LiveSessionValidationMessages.LiveSessionIdEmpty);
    }
}
