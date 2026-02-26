using FluentValidation;
using Platform.Application.Features.Lessons.Constants;
using Platform.Application.Features.Lessons.Queries.GetByIdLesson;

namespace Platform.Application.Features.Lessons.Validators;

public sealed class GetByIdLessonQueryValidator : AbstractValidator<GetByIdLessonQuery>
{
    public GetByIdLessonQueryValidator()
    {
        RuleFor(x => x.LessonId)
            .NotEmpty().WithMessage(LessonValidationMessages.LessonIdRequired);
    }
}
