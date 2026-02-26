using Platform.Domain.Common;

namespace Platform.Domain.Events;

/// <summary>
/// Event raised when a course is published.
/// </summary>
public sealed record CoursePublishedEvent(Guid CourseId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a course is archived.
/// </summary>
public sealed record CourseArchivedEvent(Guid CourseId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when course price is changed.
/// </summary>
public sealed record CoursePriceChangedEvent(Guid CourseId, decimal OldPrice, decimal NewPrice) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
