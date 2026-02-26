using FluentValidation;
using Platform.Application.Features.Lessons.Commands.CreateLesson;
using Platform.Application.Features.Lessons.Constants;

namespace Platform.Application.Features.Lessons.Validators;

public sealed class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(LessonValidationMessages.CourseIdRequired);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(LessonValidationMessages.TitleRequired)
            .MaximumLength(200).WithMessage(LessonValidationMessages.TitleMaxLength);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage(LessonValidationMessages.OrderNonNegative);

        RuleFor(x => x.VideoUrl)
            .MaximumLength(500).WithMessage(LessonValidationMessages.VideoUrlMaxLength)
            .When(x => x.VideoUrl is not null);
    }
}
