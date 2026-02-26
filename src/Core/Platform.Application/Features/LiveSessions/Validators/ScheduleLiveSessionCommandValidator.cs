using FluentValidation;
using Platform.Application.Features.LiveSessions.Commands.ScheduleLiveSession;
using Platform.Application.Features.LiveSessions.Constants;

namespace Platform.Application.Features.LiveSessions.Validators;

/// <summary>
/// Validator for ScheduleLiveSessionCommand.
/// </summary>
public sealed class ScheduleLiveSessionCommandValidator : AbstractValidator<ScheduleLiveSessionCommand>
{
    public ScheduleLiveSessionCommandValidator()
    {
        RuleFor(x => x.LessonId)
            .NotEmpty().WithMessage(LiveSessionValidationMessages.LessonIdEmpty);

        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage(LiveSessionValidationMessages.TopicRequired)
            .MinimumLength(3).WithMessage(LiveSessionValidationMessages.TopicMinLength)
            .MaximumLength(300).WithMessage(LiveSessionValidationMessages.TopicMaxLength);

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage(LiveSessionValidationMessages.StartTimeRequired)
            .Must(BeInFuture).WithMessage(LiveSessionValidationMessages.StartTimeInPast);

        RuleFor(x => x.DurationMin)
            .InclusiveBetween(15, 480).WithMessage("Duration must be between 15 and 480 minutes.");
    }

    private static bool BeInFuture(DateTime startTime)
    {
        return startTime > DateTime.UtcNow;
    }
}
