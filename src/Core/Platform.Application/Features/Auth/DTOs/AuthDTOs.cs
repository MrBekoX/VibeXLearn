namespace Platform.Application.Features.Auth.DTOs;

/// <summary>
/// DTO for registration request.
/// </summary>
public sealed record RegisterCommandDto(
    string Email,
    string Password,
    string FirstName,
    string LastName);

/// <summary>
/// DTO for login request.
/// </summary>
public sealed record LoginCommandDto(
    string Email,
    string Password);

/// <summary>
/// DTO for refresh token request.
/// </summary>
public sealed record RefreshTokenCommandDto(string RefreshToken);

/// <summary>
/// Login response — refresh token is NOT in body; it's set as HttpOnly cookie.
/// </summary>
public sealed record LoginResponseDto
{
    public string AccessToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Refresh token response — new refresh token set as HttpOnly cookie.
/// </summary>
public sealed record RefreshTokenResponseDto
{
    public string AccessToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// User profile DTO.
/// </summary>
public sealed record UserProfileDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public IEnumerable<string> Roles { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
