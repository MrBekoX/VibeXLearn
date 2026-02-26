using FluentValidation;
using Platform.Application.Features.Lessons.Commands.ReorderLessons;
using Platform.Application.Features.Lessons.Constants;

namespace Platform.Application.Features.Lessons.Validators;

public sealed class ReorderLessonsCommandValidator : AbstractValidator<ReorderLessonsCommand>
{
    public ReorderLessonsCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(LessonValidationMessages.CourseIdRequired);

        RuleFor(x => x.Lessons)
            .NotEmpty().WithMessage(LessonValidationMessages.LessonsRequired);
    }
}
