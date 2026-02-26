using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Platform.Application.Common.Constants;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Auth.Constants;
using Platform.Application.Features.Auth.DTOs;
using Platform.Domain.Entities;

#pragma warning disable CA1862 // Use StringComparison for culture-sensitive comparisons

namespace Platform.Infrastructure.Services;

/// <summary>
/// Authentication service using ASP.NET Core Identity + JWT.
/// SKILL: jwt-asymmetric-keys - Supports RS256 asymmetric key signing with rotation
/// </summary>
public sealed class AuthService(
    UserManager<AppUser> userManager,
    IConfiguration config,
    ITokenBlacklistService tokenBlacklist,
    KeyRotationService keyRotationService,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<Result<Guid>> RegisterAsync(
        string email, string password, string firstName, string lastName,
        CancellationToken ct)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return Result.Fail<Guid>("AUTH_EMAIL_EXISTS", AuthBusinessMessages.EmailAlreadyExists);

        var user = AppUser.Create(email, firstName, lastName);

        var identityResult = await userManager.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            logger.LogWarning("Registration failed for {Email}: {Errors}", email, errors);
            return Result.Fail<Guid>("AUTH_REGISTRATION_FAILED", AuthBusinessMessages.RegistrationFailed);
        }

        await userManager.AddToRoleAsync(user, "Student");

        return Result.Success(user.Id);
    }

    public async Task<Result<(string AccessToken, DateTime ExpiresAt, string RefreshToken)>> LoginAsync(
        string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.IsDeleted)
            return Result.Fail<(string, DateTime, string)>(
                "AUTH_INVALID_CREDENTIALS", AuthBusinessMessages.InvalidCredentials);

        if (await userManager.IsLockedOutAsync(user))
            return Result.Fail<(string, DateTime, string)>(
                "AUTH_ACCOUNT_LOCKED", AuthBusinessMessages.AccountLocked);

        var passwordValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);
            return Result.Fail<(string, DateTime, string)>(
                "AUTH_INVALID_CREDENTIALS", AuthBusinessMessages.InvalidCredentials);
        }

        // Reset failed access count on successful login
        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = GenerateAccessToken(user, roles);
        var refreshTokenString = GenerateRefreshTokenString();

        var refreshToken = RefreshToken.Create(user.Id, refreshTokenString, RefreshTokenLifetime);
        user.AddRefreshToken(refreshToken);
        await userManager.UpdateAsync(user);

        logger.LogInformation("{Event} | UserId:{UserId} | KeyId:{KeyId}",
            SecurityAuditEvents.LoginSuccess, user.Id, keyRotationService.CurrentKeyId);

        return Result.Success((accessToken, expiresAt, refreshTokenString));
    }

    public async Task<Result<(string AccessToken, DateTime ExpiresAt, string NewRefreshToken)>> RefreshAsync(
        string refreshToken, CancellationToken ct)
    {
        // Token-centric query: fetch only the matching refresh token + owner user.
        var tokenEntry = await userManager.Users
            .Where(u => !u.IsDeleted)
            .SelectMany(
                u => u.RefreshTokens.Where(t => t.Token == refreshToken),
                (u, t) => new { User = u, Token = t })
            .FirstOrDefaultAsync(ct);

        if (tokenEntry is null)
            return Result.Fail<(string, DateTime, string)>(
                "AUTH_INVALID_REFRESH_TOKEN", AuthBusinessMessages.InvalidRefreshToken);

        var user = tokenEntry.User;
        var existingToken = tokenEntry.Token;
        if (!existingToken.IsActive)
            return Result.Fail<(string, DateTime, string)>(
                "AUTH_INVALID_REFRESH_TOKEN", AuthBusinessMessages.InvalidRefreshToken);

        // Rotate: revoke old token, create new one
        var newRefreshTokenString = GenerateRefreshTokenString();
        existingToken.Revoke();

        var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenString, RefreshTokenLifetime);
        user.AddRefreshToken(newRefreshToken);

        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = GenerateAccessToken(user, roles);

        logger.LogInformation("{Event} | UserId:{UserId} | KeyId:{KeyId}",
            SecurityAuditEvents.TokenRefreshed, user.Id, keyRotationService.CurrentKeyId);

        return Result.Success((accessToken, expiresAt, newRefreshTokenString));
    }

    public async Task<Result> LogoutAsync(Guid userId, string? currentJti, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail("AUTH_USER_NOT_FOUND", AuthBusinessMessages.UserNotFound);

        user.RevokeAllRefreshTokens();
        await userManager.UpdateAsync(user);

        // Blacklist current access token (if JTI provided)
        if (!string.IsNullOrEmpty(currentJti))
        {
            await tokenBlacklist.BlacklistAsync(currentJti, AccessTokenLifetime, ct);

            logger.LogInformation("{Event} | UserId:{UserId} | Jti:{Jti}",
                SecurityAuditEvents.TokenRevoked, userId, currentJti[..Math.Min(8, currentJti.Length)] + "***");
        }

        return Result.Success();
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return Result.Fail<UserProfileDto>("AUTH_USER_NOT_FOUND", AuthBusinessMessages.UserNotFound);

        var roles = await userManager.GetRolesAsync(user);

        return Result.Success(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Roles = roles,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Generates JWT access token using KeyRotationService for signing.
    /// Supports both RS256 (asymmetric) and HS256 (symmetric) algorithms.
    /// SKILL: jwt-asymmetric-keys
    /// </summary>
    private (string Token, DateTime ExpiresAt) GenerateAccessToken(AppUser user, IList<string> roles)
    {
        var issuer = config["Jwt:Issuer"] ?? "VibeXLearnPlatform";
        var audience = config["Jwt:Audience"] ?? "VibeXLearnPlatform.Clients";
        var expiresAt = DateTime.UtcNow.Add(AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("kid", keyRotationService.CurrentKeyId), // Key ID for rotation support
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: keyRotationService.CurrentSigningCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static string GenerateRefreshTokenString()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
