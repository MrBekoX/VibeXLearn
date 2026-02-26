using Platform.Domain.Common;

namespace Platform.Domain.Events;

/// <summary>
/// Event raised when a submission is created.
/// </summary>
public sealed record SubmissionCreatedEvent(Guid SubmissionId, Guid StudentId, Guid LessonId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a submission is accepted.
/// </summary>
public sealed record SubmissionAcceptedEvent(Guid SubmissionId, Guid StudentId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a submission is rejected.
/// </summary>
public sealed record SubmissionRejectedEvent(Guid SubmissionId, Guid StudentId, string? ReviewNote) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
