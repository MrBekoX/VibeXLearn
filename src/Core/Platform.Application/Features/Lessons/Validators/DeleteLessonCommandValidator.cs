using FluentValidation;
using Platform.Application.Features.Lessons.Commands.DeleteLesson;
using Platform.Application.Features.Lessons.Constants;

namespace Platform.Application.Features.Lessons.Validators;

public sealed class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
{
    public DeleteLessonCommandValidator()
    {
        RuleFor(x => x.LessonId)
            .NotEmpty().WithMessage(LessonValidationMessages.LessonIdRequired);
    }
}
