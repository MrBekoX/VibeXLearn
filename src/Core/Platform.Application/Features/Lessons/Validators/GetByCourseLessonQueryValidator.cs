using FluentValidation;
using Platform.Application.Features.Lessons.Constants;
using Platform.Application.Features.Lessons.Queries.GetByCourseLesson;

namespace Platform.Application.Features.Lessons.Validators;

public sealed class GetByCourseLessonQueryValidator : AbstractValidator<GetByCourseLessonQuery>
{
    public GetByCourseLessonQueryValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(LessonValidationMessages.CourseIdRequired);
    }
}
