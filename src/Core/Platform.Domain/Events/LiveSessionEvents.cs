using Platform.Domain.Common;

namespace Platform.Domain.Events;

/// <summary>
/// Event raised when a live session is scheduled.
/// </summary>
public sealed record LiveSessionScheduledEvent(Guid LiveSessionId, Guid LessonId, DateTime StartTime) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a live session starts.
/// </summary>
public sealed record LiveSessionStartedEvent(Guid LiveSessionId, string MeetingId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a live session ends.
/// </summary>
public sealed record LiveSessionEndedEvent(Guid LiveSessionId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
