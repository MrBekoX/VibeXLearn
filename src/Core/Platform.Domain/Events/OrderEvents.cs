using Platform.Domain.Common;

namespace Platform.Domain.Events;

/// <summary>
/// Event raised when an order is created.
/// </summary>
public sealed record OrderCreatedEvent(Guid OrderId, Guid UserId, Guid CourseId, decimal Amount) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an order is paid.
/// </summary>
public sealed record OrderPaidEvent(Guid OrderId, Guid UserId, Guid CourseId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an order payment fails.
/// </summary>
public sealed record OrderFailedEvent(Guid OrderId, string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
