using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.LiveSessions.Commands.CancelLiveSession;

/// <summary>
/// Command to cancel a live session.
/// </summary>
public sealed record CancelLiveSessionCommand(Guid LiveSessionId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["livesessions:*"];
}
