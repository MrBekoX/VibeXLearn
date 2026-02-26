namespace Platform.Application.Features.Auth.Constants;

/// <summary>
/// Auth business rule messages.
/// </summary>
public static class AuthBusinessMessages
{
    public const string EmailAlreadyExists = "A user with this email already exists.";
    public const string UserNotFound = "User not found.";
    public const string InvalidCredentials = "Invalid email or password.";
    public const string AccountLocked = "Account is locked. Please try again later.";
    public const string AccountDeleted = "This account has been deactivated.";
    public const string InvalidRefreshToken = "Invalid or expired refresh token.";
    public const string RefreshTokenRevoked = "Refresh token has been revoked.";
    public const string RegistrationFailed = "Registration failed. Please try again.";
    public const string LogoutSuccess = "Successfully logged out.";
}
