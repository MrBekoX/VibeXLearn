using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Badges.Commands.CreateBadge;

/// <summary>
/// Command to create a new badge.
/// </summary>
public sealed record CreateBadgeCommand(
    string Name,
    string Description,
    string IconUrl,
    string Criteria) : IRequest<Result<Guid>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["badges:*"];
}
