using System.Text.Json;
using FluentValidation;
using Platform.Application.Features.Badges.Commands.CreateBadge;
using Platform.Application.Features.Badges.Constants;

namespace Platform.Application.Features.Badges.Validators;

/// <summary>
/// Validator for CreateBadgeCommand.
/// </summary>
public sealed class CreateBadgeCommandValidator : AbstractValidator<CreateBadgeCommand>
{
    public CreateBadgeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(BadgeValidationMessages.NameRequired)
            .MinimumLength(3).WithMessage(BadgeValidationMessages.NameMinLength)
            .MaximumLength(100).WithMessage(BadgeValidationMessages.NameMaxLength);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage(BadgeValidationMessages.DescriptionRequired)
            .MaximumLength(500).WithMessage(BadgeValidationMessages.DescriptionMaxLength);

        RuleFor(x => x.IconUrl)
            .NotEmpty().WithMessage(BadgeValidationMessages.IconUrlRequired)
            .MaximumLength(500).WithMessage(BadgeValidationMessages.IconUrlMaxLength)
            .Must(BeAValidUrl).WithMessage(BadgeValidationMessages.IconUrlInvalidFormat);

        RuleFor(x => x.Criteria)
            .NotEmpty().WithMessage(BadgeValidationMessages.CriteriaRequired)
            .Must(BeValidJson).WithMessage(BadgeValidationMessages.CriteriaInvalidJson);
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
