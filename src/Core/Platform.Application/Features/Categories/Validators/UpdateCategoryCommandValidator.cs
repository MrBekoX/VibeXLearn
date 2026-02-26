using FluentValidation;
using Platform.Application.Features.Categories.Commands.UpdateCategory;
using Platform.Application.Features.Categories.Constants;

namespace Platform.Application.Features.Categories.Validators;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage(CategoryValidationMessages.CategoryIdRequired);

        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage(CategoryValidationMessages.NameMaxLength)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage(CategoryValidationMessages.DescriptionMaxLength)
            .When(x => x.Description is not null);
    }
}
