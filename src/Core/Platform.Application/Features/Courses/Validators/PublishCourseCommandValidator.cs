using FluentValidation;
using Platform.Application.Features.Courses.Commands.PublishCourse;
using Platform.Application.Features.Courses.Constants;

namespace Platform.Application.Features.Courses.Validators;

/// <summary>
/// Validator for PublishCourseCommand.
/// </summary>
public sealed class PublishCourseCommandValidator : AbstractValidator<PublishCourseCommand>
{
    public PublishCourseCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(CourseValidationMessages.CourseIdRequired);
    }
}
