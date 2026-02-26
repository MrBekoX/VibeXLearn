using Microsoft.AspNetCore.Identity;
using Platform.Domain.Common;

namespace Platform.Domain.Entities;

/// <summary>
/// Application role with basic lifecycle behavior.
/// </summary>
public class AppRole : IdentityRole<Guid>
{
    private const int MaxRoleNameLength = 256;

    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    public static AppRole Create(string name)
    {
        var normalizedName = NormalizeRoleName(name, out var trimmedName);

        return new AppRole
        {
            Name = trimmedName,
            NormalizedName = normalizedName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Rename(string name)
    {
        var normalizedName = NormalizeRoleName(name, out var trimmedName);

        if (NormalizedName == normalizedName)
            return;

        Name = trimmedName;
        NormalizedName = normalizedName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeRoleName(string name, out string trimmedName)
    {
        trimmedName = Guard.Against.NullOrWhiteSpace(name, nameof(name)).Trim();

        if (trimmedName.Length > MaxRoleNameLength)
            throw new DomainException(
                $"Role name cannot exceed {MaxRoleNameLength} characters.");

        return trimmedName.ToUpperInvariant();
    }
}
