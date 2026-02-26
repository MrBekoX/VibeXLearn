namespace Platform.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots.
/// Only aggregate roots can have domain events that are dispatched.
/// SKILL: fix-domain-architecture
/// </summary>
public interface IAggregateRoot { }

/// <summary>
/// Base class for aggregate root entities.
/// Aggregate roots are the only entities that can be loaded/updated independently.
/// SKILL: fix-domain-architecture
/// </summary>
public abstract class AggregateRoot : BaseEntity, IAggregateRoot
{
    // Domain events are inherited from BaseEntity
    // This class serves as a marker for DDD aggregate boundaries
}
