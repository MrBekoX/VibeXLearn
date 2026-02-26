namespace Platform.Application.Features.Auth.Constants;

/// <summary>
/// Auth validation messages.
/// </summary>
public static class AuthValidationMessages
{
    // Email
    public const string EmailRequired = "Email address is required.";
    public const string EmailInvalid = "Please provide a valid email address.";
    public const string EmailMaxLength = "Email cannot exceed 256 characters.";

    // Password
    public const string PasswordRequired = "Password is required.";
    public const string PasswordMinLength = "Password must be at least 8 characters.";
    public const string PasswordMaxLength = "Password cannot exceed 128 characters.";
    public const string PasswordRequiresLetter = "Password must contain at least one letter.";
    public const string PasswordRequiresDigit = "Password must contain at least one digit.";

    // Name
    public const string FirstNameRequired = "First name is required.";
    public const string FirstNameMaxLength = "First name cannot exceed 100 characters.";
    public const string LastNameRequired = "Last name is required.";
    public const string LastNameMaxLength = "Last name cannot exceed 100 characters.";

    // RefreshToken
    public const string RefreshTokenRequired = "Refresh token is required.";

    // UserId
    public const string UserIdRequired = "User ID is required.";
    public const string UserIdEmpty = "User ID cannot be empty.";
}
