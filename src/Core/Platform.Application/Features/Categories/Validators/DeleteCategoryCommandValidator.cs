using FluentValidation;
using Platform.Application.Features.Categories.Commands.DeleteCategory;
using Platform.Application.Features.Categories.Constants;

namespace Platform.Application.Features.Categories.Validators;

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage(CategoryValidationMessages.CategoryIdRequired);
    }
}
