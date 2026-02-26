using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.LiveSessions.DTOs;

namespace Platform.Application.Features.LiveSessions.Commands.StartLiveSession;

/// <summary>
/// Command to start a live session.
/// </summary>
public sealed record StartLiveSessionCommand(Guid LiveSessionId) : IRequest<Result<StartLiveSessionResponseDto>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["livesessions:*"];
}
