using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Badges.Commands.UpdateBadge;

/// <summary>
/// Command to update an existing badge.
/// </summary>
public sealed record UpdateBadgeCommand(
    Guid BadgeId,
    string? Name = null,
    string? Description = null,
    string? IconUrl = null) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["badges:*"];
}
