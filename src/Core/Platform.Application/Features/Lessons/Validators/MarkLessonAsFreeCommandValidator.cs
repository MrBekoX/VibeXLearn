using FluentValidation;
using Platform.Application.Features.Lessons.Commands.MarkLessonAsFree;
using Platform.Application.Features.Lessons.Constants;

namespace Platform.Application.Features.Lessons.Validators;

public sealed class MarkLessonAsFreeCommandValidator : AbstractValidator<MarkLessonAsFreeCommand>
{
    public MarkLessonAsFreeCommandValidator()
    {
        RuleFor(x => x.LessonId)
            .NotEmpty().WithMessage(LessonValidationMessages.LessonIdRequired);
    }
}
