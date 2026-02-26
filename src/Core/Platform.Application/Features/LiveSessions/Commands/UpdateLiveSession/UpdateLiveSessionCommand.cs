using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.LiveSessions.Commands.UpdateLiveSession;

/// <summary>
/// Command to update a scheduled live session.
/// </summary>
public sealed record UpdateLiveSessionCommand(
    Guid LiveSessionId,
    string? Topic = null,
    DateTime? StartTime = null,
    int? DurationMin = null) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["livesessions:*"];
}
