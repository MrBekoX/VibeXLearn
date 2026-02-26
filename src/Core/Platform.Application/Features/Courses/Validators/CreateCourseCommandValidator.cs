using FluentValidation;
using Platform.Application.Features.Courses.Commands.CreateCourse;
using Platform.Application.Features.Courses.Constants;

namespace Platform.Application.Features.Courses.Validators;

/// <summary>
/// Validator for CreateCourseCommand.
/// </summary>
public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(CourseValidationMessages.TitleRequired)
            .MaximumLength(200).WithMessage(CourseValidationMessages.TitleMaxLength);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage(CourseValidationMessages.SlugRequired)
            .MaximumLength(200).WithMessage(CourseValidationMessages.SlugMaxLength)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage(CourseValidationMessages.SlugInvalidFormat);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage(CourseValidationMessages.PricePositive)
            .LessThanOrEqualTo(99_999.99m).WithMessage(CourseValidationMessages.PriceMax);

        RuleFor(x => x.InstructorId)
            .NotEmpty().WithMessage(CourseValidationMessages.InstructorRequired);

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage(CourseValidationMessages.CategoryRequired);

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage(CourseValidationMessages.DescriptionMaxLength)
            .When(x => x.Description is not null);
    }
}
