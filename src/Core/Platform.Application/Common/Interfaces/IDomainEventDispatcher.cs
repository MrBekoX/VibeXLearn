using Platform.Domain.Common;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Dispatches domain events to their handlers.
/// SKILL: fix-domain-architecture
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches the event to all registered handlers.
    /// </summary>
    Task DispatchAsync(IDomainEvent @event, CancellationToken ct = default);

    /// <summary>
    /// Dispatches all domain events from the aggregate root.
    /// </summary>
    Task DispatchAllAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
