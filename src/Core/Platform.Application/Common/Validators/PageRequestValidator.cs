using FluentValidation;
using Platform.Application.Common.Models.Pagination;

namespace Platform.Application.Common.Validators;

/// <summary>
/// Validator for PageRequest pagination parameters.
/// SKILL: pagination-limits
/// </summary>
public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .WithMessage("Search term cannot exceed 200 characters");

        RuleFor(x => x.Sort)
            .MaximumLength(500)
            .WithMessage("Sort expression cannot exceed 500 characters");
    }
}
