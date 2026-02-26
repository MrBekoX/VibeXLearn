using FluentValidation;
using Platform.Application.Features.Courses.Commands.UpdateCourse;
using Platform.Application.Features.Courses.Constants;

namespace Platform.Application.Features.Courses.Validators;

/// <summary>
/// Validator for UpdateCourseCommand.
/// </summary>
public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(CourseValidationMessages.CourseIdRequired);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(CourseValidationMessages.TitleRequired)
            .MaximumLength(200).WithMessage(CourseValidationMessages.TitleMaxLength)
            .When(x => x.Title is not null);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage(CourseValidationMessages.PricePositive)
            .LessThanOrEqualTo(99_999.99m).WithMessage(CourseValidationMessages.PriceMax)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage(CourseValidationMessages.CategoryRequired)
            .When(x => x.CategoryId.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage(CourseValidationMessages.DescriptionMaxLength)
            .When(x => x.Description is not null);
    }
}
