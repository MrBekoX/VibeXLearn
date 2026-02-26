using Platform.Application.Common.Results;
using Platform.Application.Features.Auth.DTOs;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Authentication service contract.
/// Implementation lives in Infrastructure layer (DIP).
/// SKILL: jwt-asymmetric-keys - Supports key rotation
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user with Student role.
    /// </summary>
    Task<Result<Guid>> RegisterAsync(
        string email, string password, string firstName, string lastName,
        CancellationToken ct);

    /// <summary>
    /// Authenticate user and return JWT access token + refresh token.
    /// </summary>
    Task<Result<(string AccessToken, DateTime ExpiresAt, string RefreshToken)>> LoginAsync(
        string email, string password,
        CancellationToken ct);

    /// <summary>
    /// Rotate refresh token and issue a new access token.
    /// </summary>
    Task<Result<(string AccessToken, DateTime ExpiresAt, string NewRefreshToken)>> RefreshAsync(
        string refreshToken,
        CancellationToken ct);

    /// <summary>
    /// Revoke all refresh tokens for the user and blacklist current access token.
    /// </summary>
    Task<Result> LogoutAsync(Guid userId, string? currentJti, CancellationToken ct);

    /// <summary>
    /// Get user profile with roles.
    /// </summary>
    Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct);
}
