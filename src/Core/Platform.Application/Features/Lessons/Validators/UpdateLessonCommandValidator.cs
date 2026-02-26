using FluentValidation;
using Platform.Application.Features.Lessons.Commands.UpdateLesson;
using Platform.Application.Features.Lessons.Constants;

namespace Platform.Application.Features.Lessons.Validators;

public sealed class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.LessonId)
            .NotEmpty().WithMessage(LessonValidationMessages.LessonIdRequired);

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage(LessonValidationMessages.TitleMaxLength)
            .When(x => x.Title is not null);

        RuleFor(x => x.VideoUrl)
            .MaximumLength(500).WithMessage(LessonValidationMessages.VideoUrlMaxLength)
            .When(x => x.VideoUrl is not null);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage(LessonValidationMessages.OrderNonNegative)
            .When(x => x.Order.HasValue);
    }
}
