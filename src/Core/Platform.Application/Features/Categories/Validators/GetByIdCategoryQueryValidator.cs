using FluentValidation;
using Platform.Application.Features.Categories.Constants;
using Platform.Application.Features.Categories.Queries.GetByIdCategory;

namespace Platform.Application.Features.Categories.Validators;

public sealed class GetByIdCategoryQueryValidator : AbstractValidator<GetByIdCategoryQuery>
{
    public GetByIdCategoryQueryValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage(CategoryValidationMessages.CategoryIdRequired);
    }
}
