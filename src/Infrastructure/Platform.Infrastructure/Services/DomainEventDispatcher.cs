using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Interfaces;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Dispatches domain events using MediatR.
/// Collects handler failures and re-throws after all handlers run.
/// </summary>
public sealed class DomainEventDispatcher(
    IPublisher publisher,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        try
        {
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
            throw;
        }
    }

    public async Task DispatchAllAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        List<Exception>? exceptions = null;

        foreach (var @event in events)
        {
            try
            {
                await DispatchAsync(@event, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Handler failure for domain event {EventType}; continuing with remaining events",
                    @event.GetType().Name);
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
            throw new AggregateException(
                "One or more domain event handlers failed.", exceptions);
    }
}

/// <summary>
/// MediatR notification wrapper for domain events.
/// Allows domain events to be handled by INotificationHandler implementations.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent Event) : INotification;
