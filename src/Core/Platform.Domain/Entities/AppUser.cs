using Microsoft.AspNetCore.Identity;
using Platform.Domain.Common;

namespace Platform.Domain.Entities;

/// <summary>
/// AppUser aggregate with profile management.
/// Note: IdentityUser already has Id, UserName, Email, etc.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string    FirstName  { get; set; } = default!;
    public string    LastName   { get; set; } = default!;
    public string?   AvatarUrl  { get; set; }
    public string?   Bio        { get; set; }
    public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt  { get; set; }
    public bool      IsDeleted  { get; set; } = false;
    public DateTime? DeletedAt  { get; set; }

    // Computed properties
    public string    FullName   => $"{FirstName} {LastName}".Trim();
    public string    Initials   => $"{FirstName?[..1]}{LastName?[..1]}".ToUpperInvariant();

    // Navigation properties
    public ICollection<RefreshToken>  RefreshTokens  { get; set; } = [];
    public ICollection<Enrollment>    Enrollments    { get; set; } = [];
    public ICollection<Order>         Orders         { get; set; } = [];
    public ICollection<UserBadge>     UserBadges     { get; set; } = [];
    public ICollection<Certificate>   Certificates   { get; set; } = [];
    public ICollection<Submission>    Submissions    { get; set; } = [];

    /// <summary>
    /// Factory method to create a new user.
    /// </summary>
    public static AppUser Create(string email, string firstName, string lastName)
    {
        Guard.Against.NullOrWhiteSpace(email, nameof(email));
        Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
        Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));

        return new AppUser
        {
            UserName = email.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Update profile information.
    /// </summary>
    public void UpdateProfile(string firstName, string lastName, string? bio = null)
    {
        FirstName = Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName)).Trim();
        LastName = Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName)).Trim();
        Bio = bio?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update avatar URL.
    /// </summary>
    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update bio.
    /// </summary>
    public void UpdateBio(string? bio)
    {
        Bio = bio?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete the user.
    /// </summary>
    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restore soft deleted user.
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a refresh token.
    /// </summary>
    public void AddRefreshToken(RefreshToken token)
    {
        RefreshTokens.Add(token);
    }

    /// <summary>
    /// Revoke all refresh tokens.
    /// </summary>
    public void RevokeAllRefreshTokens()
    {
        foreach (var token in RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get active refresh token.
    /// </summary>
    public RefreshToken? GetActiveRefreshToken(string token)
    {
        return RefreshTokens.FirstOrDefault(t => t.Token == token && t.IsActive);
    }
}
