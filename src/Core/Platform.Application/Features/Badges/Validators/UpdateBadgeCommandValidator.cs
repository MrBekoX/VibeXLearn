using FluentValidation;
using Platform.Application.Features.Badges.Commands.UpdateBadge;
using Platform.Application.Features.Badges.Constants;

namespace Platform.Application.Features.Badges.Validators;

/// <summary>
/// Validator for UpdateBadgeCommand.
/// </summary>
public sealed class UpdateBadgeCommandValidator : AbstractValidator<UpdateBadgeCommand>
{
    public UpdateBadgeCommandValidator()
    {
        RuleFor(x => x.BadgeId)
            .NotEmpty().WithMessage(BadgeValidationMessages.BadgeIdEmpty);

        RuleFor(x => x.Name)
            .MinimumLength(3).WithMessage(BadgeValidationMessages.NameMinLength)
            .MaximumLength(100).WithMessage(BadgeValidationMessages.NameMaxLength)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage(BadgeValidationMessages.DescriptionMaxLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).WithMessage(BadgeValidationMessages.IconUrlMaxLength)
            .Must(BeAValidUrl).WithMessage(BadgeValidationMessages.IconUrlInvalidFormat)
            .When(x => x.IconUrl is not null);
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
