using FluentValidation;
using Platform.Application.Features.Badges.Constants;
using Platform.Application.Features.Badges.Queries.GetByIdBadge;

namespace Platform.Application.Features.Badges.Validators;

/// <summary>
/// Validator for GetByIdBadgeQuery.
/// </summary>
public sealed class GetByIdBadgeQueryValidator : AbstractValidator<GetByIdBadgeQuery>
{
    public GetByIdBadgeQueryValidator()
    {
        RuleFor(x => x.BadgeId)
            .NotEmpty().WithMessage(BadgeValidationMessages.BadgeIdEmpty);
    }
}
