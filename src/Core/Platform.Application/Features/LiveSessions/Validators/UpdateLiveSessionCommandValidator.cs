using FluentValidation;
using Platform.Application.Features.LiveSessions.Commands.UpdateLiveSession;
using Platform.Application.Features.LiveSessions.Constants;

namespace Platform.Application.Features.LiveSessions.Validators;

/// <summary>
/// Validator for UpdateLiveSessionCommand.
/// </summary>
public sealed class UpdateLiveSessionCommandValidator : AbstractValidator<UpdateLiveSessionCommand>
{
    public UpdateLiveSessionCommandValidator()
    {
        RuleFor(x => x.LiveSessionId)
            .NotEmpty().WithMessage(LiveSessionValidationMessages.LiveSessionIdEmpty);

        RuleFor(x => x.Topic)
            .MinimumLength(3).WithMessage(LiveSessionValidationMessages.TopicMinLength)
            .MaximumLength(300).WithMessage(LiveSessionValidationMessages.TopicMaxLength)
            .When(x => x.Topic is not null);

        RuleFor(x => x.DurationMin)
            .GreaterThanOrEqualTo(15).WithMessage(LiveSessionValidationMessages.DurationMin)
            .LessThanOrEqualTo(480).WithMessage(LiveSessionValidationMessages.DurationMax)
            .When(x => x.DurationMin.HasValue);
    }
}
