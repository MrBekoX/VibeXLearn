using Platform.Domain.Common;

namespace Platform.Domain.Events;

/// <summary>
/// Event raised when a user enrolls in a course.
/// </summary>
public sealed record EnrollmentCreatedEvent(Guid EnrollmentId, Guid UserId, Guid CourseId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an enrollment is completed.
/// </summary>
public sealed record EnrollmentCompletedEvent(Guid EnrollmentId, Guid UserId, Guid CourseId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when enrollment progress is updated.
/// </summary>
public sealed record EnrollmentProgressUpdatedEvent(Guid EnrollmentId, decimal Progress) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
