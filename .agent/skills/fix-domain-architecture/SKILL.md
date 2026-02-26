---
name: fix-domain-architecture
description: Domain katmanında IdentityUser dependency var (Onion Architecture ihlali). BaseEntity setter'ları public. Aggregate root marker yok. Domain event dispatch mekanizması yok. Bu skill, DDD best practice'leri uygular.
---

# Fix Domain Architecture Issues

## Problems

**Risk Level:** MEDIUM/HIGH

1. **Domain Not Pure** - `IdentityUser<Guid>` dependency violates Onion Architecture
2. **BaseEntity Public Setters** - Encapsulation weak
3. **No Aggregate Root Marker** - Aggregate boundaries implicit
4. **Domain Events Not Dispatched** - Events collected but never published

**Affected Files:**
- `src/Core/Platform.Domain/Platform.Domain.csproj`
- `src/Core/Platform.Domain/Common/BaseEntity.cs`
- `src/Infrastructure/Platform.Persistence/Context/AppDbContext.cs`

## Solution Steps

### Step 1: Create Aggregate Root Infrastructure

Create file: `src/Core/Platform.Domain/Common/AggregateRoot.cs`

```csharp
namespace Platform.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots.
/// Only aggregate roots can have domain events.
/// </summary>
public interface IAggregateRoot { }

/// <summary>
/// Base class for aggregate root entities.
/// Manages domain events collection.
/// </summary>
public abstract class AggregateRoot : AuditableEntity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the collection.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent @event)
    {
        _domainEvents.Add(@event);
    }

    /// <summary>
    /// Clears all domain events.
    /// Called after events are dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

### Step 2: Update Entity Inheritance

Modify entities to use correct base class:

**Aggregate Roots (use AggregateRoot):**
```csharp
// Course.cs
public sealed class Course : AggregateRoot { }

// Order.cs
public sealed class Order : AggregateRoot { }

// Enrollment.cs
public sealed class Enrollment : AggregateRoot { }

// Submission.cs
public sealed class Submission : AggregateRoot { }
```

**Child Entities (use AuditableEntity or BaseEntity):**
```csharp
// Lesson.cs - child of Course
public sealed class Lesson : AuditableEntity { }

// PaymentIntent.cs - child of Order
public sealed class PaymentIntent : BaseEntity { }

// LiveSession.cs - child of Lesson
public sealed class LiveSession : BaseEntity { }
```

### Step 3: Fix BaseEntity Setters

Modify `src/Core/Platform.Domain/Common/BaseEntity.cs`:

```csharp
namespace Platform.Domain.Common;

/// <summary>
/// Base entity with protected setters for encapsulation.
/// EF Core supports protected/internal setters via navigation.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; internal set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public DateTime? DeletedAt { get; internal set; }

    /// <summary>
    /// Marks entity as updated. Called by domain methods.
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete. Called explicitly, not by EF cascade.
    /// </summary>
    public virtual void SoftDelete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Restore soft-deleted entity.
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        MarkAsUpdated();
    }
}
```

### Step 4: Add Domain Event Dispatcher Interface

Create file: `src/Core/Platform.Application/Common/Interfaces/IDomainEventDispatcher.cs`

```csharp
using Platform.Domain.Common;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Dispatches domain events to handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches the event to all registered handlers.
    /// </summary>
    Task DispatchAsync(IDomainEvent @event, CancellationToken ct = default);
}
```

### Step 5: Implement Domain Event Dispatcher

Create file: `src/Infrastructure/Platform.Infrastructure/Services/DomainEventDispatcher.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Interfaces;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Dispatches domain events using MediatR.
/// Events are dispatched as notifications (fire-and-forget).
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
}

/// <summary>
/// MediatR notification wrapper for domain events.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent Event) : INotification;
```

### Step 6: Create Domain Event Handlers

Create file: `src/Core/Platform.Application/Features/Courses/EventHandlers/CoursePublishedEventHandler.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Domain.Events;

namespace Platform.Application.Features.Courses.EventHandlers;

/// <summary>
/// Handles CoursePublishedEvent.
/// Example: Update search index, send notification, etc.
/// </summary>
public sealed class CoursePublishedEventHandler(
    ILogger<CoursePublishedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CoursePublishedEvent>>
{
    public async Task Handle(
        DomainEventNotification<CoursePublishedEvent> notification,
        CancellationToken ct)
    {
        var @event = notification.Event;

        logger.LogInformation(
            "{Event} | CourseId:{CourseId}",
            "COURSE_PUBLISHED", @event.CourseId);

        // TODO: Implement actual handlers:
        // - Update Elasticsearch index
        // - Send notification to enrolled students
        // - Update statistics

        await Task.CompletedTask;
    }
}
```

### Step 7: Update DbContext to Dispatch Events

Modify `src/Infrastructure/Platform.Persistence/Context/AppDbContext.cs`:

```csharp
public override async Task<int> SaveChangesAsync(
    bool acceptAllChangesOnSuccess,
    CancellationToken ct = default)
{
    // Collect domain events before save
    var aggregateRoots = ChangeTracker.Entries<AggregateRoot>()
        .Where(e => e.Entity.DomainEvents.Count != 0)
        .ToList();

    var domainEvents = aggregateRoots
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    // Set audit fields
    SetAuditFields();

    // Save changes
    var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);

    // Dispatch events AFTER successful save
    if (domainEvents.Count != 0)
    {
        var dispatcher = _serviceProvider.GetService(typeof(IDomainEventDispatcher))
            as IDomainEventDispatcher;

        if (dispatcher is not null)
        {
            foreach (var @event in domainEvents)
            {
                await dispatcher.DispatchAsync(@event, ct);
            }
        }

        // Clear events after dispatch
        foreach (var aggregate in aggregateRoots)
        {
            aggregate.Entity.ClearDomainEvents();
        }
    }

    return result;
}
```

### Step 8: Register Domain Event Dispatcher

Add to `src/Infrastructure/Platform.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`:

```csharp
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```

## Verification

```csharp
// Unit test example
[Fact]
public async Task Course_Publish_Should_Raise_DomainEvent()
{
    // Arrange
    var course = Course.Create("Test", "test", 100m, CourseLevel.Beginner, Guid.NewGuid(), Guid.NewGuid());

    // Act
    course.Publish();

    // Assert
    course.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<CoursePublishedEvent>();
}
```

## Priority

**MEDIUM** - Architecture improvement, not blocking production.
