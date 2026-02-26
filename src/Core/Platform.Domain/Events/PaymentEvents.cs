using Platform.Domain.Common;

namespace Platform.Domain.Events;

/// <summary>
/// Event raised when payment is completed.
/// </summary>
public sealed record PaymentCompletedEvent(Guid OrderId, string PaymentId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when payment fails.
/// </summary>
public sealed record PaymentFailedEvent(Guid OrderId, string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when payment processing starts.
/// </summary>
public sealed record PaymentProcessingEvent(Guid OrderId, string ConversationId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
