using FluentValidation;
using Platform.Application.Features.Categories.Commands.CreateCategory;
using Platform.Application.Features.Categories.Constants;

namespace Platform.Application.Features.Categories.Validators;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(CategoryValidationMessages.NameRequired)
            .MaximumLength(100).WithMessage(CategoryValidationMessages.NameMaxLength);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage(CategoryValidationMessages.SlugRequired)
            .MaximumLength(150).WithMessage(CategoryValidationMessages.SlugMaxLength)
            .Matches("^[a-z0-9-]+$").WithMessage(CategoryValidationMessages.SlugInvalidFormat);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage(CategoryValidationMessages.DescriptionMaxLength)
            .When(x => x.Description is not null);
    }
}
