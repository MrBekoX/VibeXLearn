using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Interfaces;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Dispatches domain events using MediatR.
/// Events are dispatched as notifications (fire-and-forget).
/// SKILL: fix-domain-architecture
/// </summary>
public sealed class DomainEventDispatcher(
    IPublisher publisher,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        try
        {
            // Wrap domain event as MediatR notification
            await publisher.Publish(new DomainEventNotification(@event), ct);

            logger.LogDebug(
                "Domain event dispatched: {EventType} at {OccurredOn}",
                @event.GetType().Name, @event.OccurredOn);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to dispatch domain event: {EventType}",
                @event.GetType().Name);
            // Don't throw - event dispatch failure shouldn't break the transaction
        }
    }

    public async Task DispatchAllAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            await DispatchAsync(@event, ct);
        }
    }
}

/// <summary>
/// MediatR notification wrapper for domain events.
/// Allows domain events to be handled by INotificationHandler implementations.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent Event) : INotification;
