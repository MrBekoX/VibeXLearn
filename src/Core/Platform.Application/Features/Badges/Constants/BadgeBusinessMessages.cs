namespace Platform.Application.Features.Badges.Constants;

/// <summary>
/// Badge business rule messages.
/// </summary>
public static class BadgeBusinessMessages
{
    // Not Found
    public const string NotFound = "Badge not found.";
    public const string NotFoundById = "Badge not found with the specified ID.";

    // Validation Errors
    public const string NameAlreadyExists = "A badge with this name already exists.";
    public const string CriteriaInvalid = "Badge criteria must be valid JSON.";

    // Success Messages
    public const string CreatedSuccessfully = "Badge created successfully.";
    public const string UpdatedSuccessfully = "Badge updated successfully.";
    public const string DeletedSuccessfully = "Badge deleted successfully.";
    public const string AwardedSuccessfully = "Badge awarded to user successfully.";
}
