using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.LiveSessions.Commands.ScheduleLiveSession;

/// <summary>
/// Command to schedule a new live session.
/// </summary>
public sealed record ScheduleLiveSessionCommand(
    Guid LessonId,
    string Topic,
    DateTime StartTime,
    int DurationMin) : IRequest<Result<Guid>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["livesessions:*"];
}
