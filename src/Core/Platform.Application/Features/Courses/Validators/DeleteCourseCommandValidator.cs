using FluentValidation;
using Platform.Application.Features.Courses.Commands.DeleteCourse;
using Platform.Application.Features.Courses.Constants;

namespace Platform.Application.Features.Courses.Validators;

/// <summary>
/// Validator for DeleteCourseCommand.
/// </summary>
public sealed class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
{
    public DeleteCourseCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(CourseValidationMessages.CourseIdRequired);
    }
}
