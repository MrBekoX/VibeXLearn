using Platform.Domain.Common;

namespace Platform.Domain.Entities;

/// <summary>
/// RefreshToken with revocation and rotation logic.
/// </summary>
public class RefreshToken : BaseEntity
{
    // Private setters for encapsulation
    public Guid      UserId          { get; private set; }
    public string    Token           { get; private set; } = default!;
    public DateTime  ExpiresAt       { get; private set; }
    public bool      IsRevoked       { get; private set; } = false;
    public DateTime? RevokedAt       { get; private set; }
    public string?   ReplacedByToken { get; private set; }

    // Computed properties
    public bool      IsExpired       => DateTime.UtcNow >= ExpiresAt;
    public bool      IsActive        => !IsRevoked && !IsExpired;
    public bool      IsUsable        => IsActive;

    // Navigation properties
    public AppUser   User            { get; private set; } = default!;

    // Private constructor for EF Core
    private RefreshToken() { }

    /// <summary>
    /// Factory method to create a new refresh token.
    /// </summary>
    public static RefreshToken Create(Guid userId, string token, TimeSpan lifetime)
    {
        Guard.Against.EmptyGuid(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(token, nameof(token));

        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            IsRevoked = false
        };
    }

    /// <summary>
    /// Create refresh token with custom expiration.
    /// </summary>
    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        Guard.Against.EmptyGuid(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(token, nameof(token));

        if (expiresAt <= DateTime.UtcNow)
            throw new DomainException("REFRESH_TOKEN_EXPIRED",
                "Expiration date must be in the future.");

        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt.Kind == DateTimeKind.Utc ? expiresAt : expiresAt.ToUniversalTime(),
            IsRevoked = false
        };
    }

    /// <summary>
    /// Revoke the token.
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked)
            return;

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Revoke and mark as replaced by a new token (rotation).
    /// </summary>
    public void RevokeAndReplace(string newToken)
    {
        if (IsRevoked)
            throw new DomainException("REFRESH_TOKEN_ALREADY_REVOKED",
                "Token is already revoked.");

        Guard.Against.NullOrWhiteSpace(newToken, nameof(newToken));

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = newToken;
        MarkAsUpdated();
    }

    /// <summary>
    /// Extend expiration (use with caution).
    /// </summary>
    public void ExtendExpiration(TimeSpan additionalTime)
    {
        if (IsRevoked)
            throw new DomainException("REFRESH_TOKEN_REVOKED",
                "Cannot extend revoked token.");

        ExpiresAt = ExpiresAt.Add(additionalTime);
        MarkAsUpdated();
    }

    /// <summary>
    /// Check if token was used after being replaced (potential token reuse attack).
    /// </summary>
    public bool WasReplacedAfter(DateTime usageTime)
    {
        return ReplacedByToken is not null &&
               RevokedAt.HasValue &&
               usageTime > RevokedAt.Value;
    }
}
