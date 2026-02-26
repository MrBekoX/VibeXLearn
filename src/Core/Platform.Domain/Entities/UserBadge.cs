using Platform.Domain.Common;

namespace Platform.Domain.Entities;

/// <summary>
/// UserBadge - earned badge association.
/// </summary>
public class UserBadge : BaseEntity
{
    // Private setters for encapsulation
    public Guid     UserId   { get; private set; }
    public Guid     BadgeId  { get; private set; }
    public DateTime EarnedAt { get; private set; }

    // Navigation properties
    public AppUser  User     { get; private set; } = default!;
    public Badge    Badge    { get; private set; } = default!;

    // Private constructor for EF Core
    private UserBadge() { }

    /// <summary>
    /// Factory method to award a badge to a user.
    /// </summary>
    public static UserBadge Award(Guid userId, Guid badgeId)
    {
        Guard.Against.EmptyGuid(userId, nameof(userId));
        Guard.Against.EmptyGuid(badgeId, nameof(badgeId));

        return new UserBadge
        {
            UserId = userId,
            BadgeId = badgeId,
            EarnedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Factory method to award a badge to a user with custom earned date.
    /// </summary>
    public static UserBadge Award(Guid userId, Guid badgeId, DateTime earnedAt)
    {
        var userBadge = Award(userId, badgeId);
        userBadge.EarnedAt = earnedAt.Kind == DateTimeKind.Utc ? earnedAt : earnedAt.ToUniversalTime();
        return userBadge;
    }
}
