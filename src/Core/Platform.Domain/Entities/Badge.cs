using Platform.Domain.Common;

namespace Platform.Domain.Entities;

/// <summary>
/// Badge with criteria evaluation.
/// </summary>
public class Badge : BaseEntity
{
    // Private setters for encapsulation
    public string   Name        { get; private set; } = default!;
    public string   Description { get; private set; } = default!;
    public string   IconUrl     { get; private set; } = default!;
    public string   Criteria    { get; private set; } = default!;  // JSONB kural tanımı

    // Navigation properties
    public ICollection<UserBadge> UserBadges { get; private set; } = [];

    // Private constructor for EF Core
    private Badge() { }

    /// <summary>
    /// Factory method to create a new badge.
    /// </summary>
    public static Badge Create(string name, string description, string iconUrl, string criteria)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(iconUrl, nameof(iconUrl));
        Guard.Against.NullOrWhiteSpace(criteria, nameof(criteria));

        return new Badge
        {
            Name = name.Trim(),
            Description = description.Trim(),
            IconUrl = iconUrl.Trim(),
            Criteria = criteria
        };
    }

    /// <summary>
    /// Create a badge for enrollment count threshold.
    /// </summary>
    public static Badge CreateEnrollmentBadge(string name, string description, string iconUrl, int threshold)
    {
        var criteria = $@"{{""type"":""enrollment_count"",""threshold"":{threshold}}}";
        return Create(name, description, iconUrl, criteria);
    }

    /// <summary>
    /// Create a badge for course completion count threshold.
    /// </summary>
    public static Badge CreateCompletionBadge(string name, string description, string iconUrl, int threshold)
    {
        var criteria = $@"{{""type"":""completion_count"",""threshold"":{threshold}}}";
        return Create(name, description, iconUrl, criteria);
    }

    /// <summary>
    /// Update badge details.
    /// </summary>
    public void UpdateDetails(string name, string description)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)).Trim();
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description)).Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update badge icon.
    /// </summary>
    public void UpdateIcon(string iconUrl)
    {
        IconUrl = Guard.Against.NullOrWhiteSpace(iconUrl, nameof(iconUrl)).Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update badge criteria.
    /// </summary>
    public void UpdateCriteria(string criteria)
    {
        Criteria = Guard.Against.NullOrWhiteSpace(criteria, nameof(criteria));
        MarkAsUpdated();
    }
}
