namespace Platform.Application.Features.Badges.DTOs;

/// <summary>
/// DTO for badge list items.
/// </summary>
public sealed record GetAllBadgesQueryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string IconUrl { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for detailed badge information.
/// </summary>
public sealed record GetByIdBadgeQueryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string IconUrl { get; init; } = default!;
    public string Criteria { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int UserCount { get; init; }
}

/// <summary>
/// DTO for badge creation request.
/// </summary>
public sealed record CreateBadgeCommandDto
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string IconUrl { get; init; } = default!;
    public string Criteria { get; init; } = default!;
}

/// <summary>
/// DTO for badge update request.
/// </summary>
public sealed record UpdateBadgeCommandDto
{
    public Guid BadgeId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
}
