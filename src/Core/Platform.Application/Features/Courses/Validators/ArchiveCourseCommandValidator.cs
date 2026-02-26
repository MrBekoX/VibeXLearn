using FluentValidation;
using Platform.Application.Features.Courses.Commands.ArchiveCourse;
using Platform.Application.Features.Courses.Constants;

namespace Platform.Application.Features.Courses.Validators;

/// <summary>
/// Validator for ArchiveCourseCommand.
/// </summary>
public sealed class ArchiveCourseCommandValidator : AbstractValidator<ArchiveCourseCommand>
{
    public ArchiveCourseCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(CourseValidationMessages.CourseIdRequired);
    }
}
