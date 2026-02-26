using FluentValidation;
using Platform.Application.Features.LiveSessions.Commands.EndLiveSession;
using Platform.Application.Features.LiveSessions.Constants;

namespace Platform.Application.Features.LiveSessions.Validators;

/// <summary>
/// Validator for EndLiveSessionCommand.
/// </summary>
public sealed class EndLiveSessionCommandValidator : AbstractValidator<EndLiveSessionCommand>
{
    public EndLiveSessionCommandValidator()
    {
        RuleFor(x => x.LiveSessionId)
            .NotEmpty().WithMessage(LiveSessionValidationMessages.LiveSessionIdEmpty);
    }
}
