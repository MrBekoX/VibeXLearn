namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Service for accessing the current authenticated user's information.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Returns the current user's info from the HTTP context claims.
    /// </summary>
    CurrentUserDto GetCurrentUser();

    /// <summary>
    /// Returns the current user's ID. Throws if not authenticated.
    /// </summary>
    Guid GetUserId();

    /// <summary>
    /// Returns whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated();

    /// <summary>
    /// Returns whether the current user is in the specified role.
    /// </summary>
    bool IsInRole(string role);
}

/// <summary>
/// Lightweight DTO representing the authenticated user.
/// Populated from JWT claims — no DB round-trip.
/// </summary>
public sealed record CurrentUserDto(
    Guid   UserId,
    string Email,
    string FirstName,
    string LastName)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}
