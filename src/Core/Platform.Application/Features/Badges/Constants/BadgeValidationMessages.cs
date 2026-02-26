namespace Platform.Application.Features.Badges.Constants;

/// <summary>
/// Badge validation messages.
/// </summary>
public static class BadgeValidationMessages
{
    // ID
    public const string BadgeIdRequired = "Badge ID is required.";
    public const string BadgeIdEmpty = "Badge ID cannot be empty.";

    // Name
    public const string NameRequired = "Badge name is required.";
    public const string NameMaxLength = "Badge name cannot exceed 100 characters.";
    public const string NameMinLength = "Badge name must be at least 3 characters.";

    // Description
    public const string DescriptionRequired = "Badge description is required.";
    public const string DescriptionMaxLength = "Badge description cannot exceed 500 characters.";

    // Icon URL
    public const string IconUrlRequired = "Icon URL is required.";
    public const string IconUrlMaxLength = "Icon URL cannot exceed 500 characters.";
    public const string IconUrlInvalidFormat = "Icon URL must be a valid URL.";

    // Criteria
    public const string CriteriaRequired = "Badge criteria is required.";
    public const string CriteriaInvalidJson = "Badge criteria must be valid JSON.";
}
