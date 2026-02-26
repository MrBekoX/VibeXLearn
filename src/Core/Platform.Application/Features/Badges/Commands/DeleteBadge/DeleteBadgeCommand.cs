using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Badges.Commands.DeleteBadge;

/// <summary>
/// Command to delete a badge (soft delete).
/// </summary>
public sealed record DeleteBadgeCommand(Guid BadgeId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["badges:*"];
}
