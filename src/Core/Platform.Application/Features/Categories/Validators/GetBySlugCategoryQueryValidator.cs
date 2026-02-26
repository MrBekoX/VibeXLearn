using FluentValidation;
using Platform.Application.Features.Categories.Constants;
using Platform.Application.Features.Categories.Queries.GetBySlugCategory;

namespace Platform.Application.Features.Categories.Validators;

public sealed class GetBySlugCategoryQueryValidator : AbstractValidator<GetBySlugCategoryQuery>
{
    public GetBySlugCategoryQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage(CategoryValidationMessages.SlugRequired)
            .MaximumLength(150).WithMessage(CategoryValidationMessages.SlugMaxLength);
    }
}
