using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.LiveSessions.Commands.EndLiveSession;

/// <summary>
/// Command to end a live session.
/// </summary>
public sealed record EndLiveSessionCommand(Guid LiveSessionId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["livesessions:*"];
}
