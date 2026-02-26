namespace Platform.Domain.Common;

/// <summary>
/// Tüm entity'ler için base sınıf. Audit alanlarını ve domain events içerir.
/// ENCAPSULATION: Setter'lar protected - EF Core ile internal metodlar üzerinden çalışır.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid      Id        { get; protected set; } = Guid.NewGuid();
    public DateTime  CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool      IsDeleted { get; protected set; } = false;
    public DateTime? DeletedAt { get; protected set; }

    /// <summary>
    /// Domain events to be dispatched after save.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Add a domain event.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clear all domain events (called after dispatch).
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Mark entity as updated.
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete the entity.
    /// </summary>
    public virtual void SoftDelete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Restore soft deleted entity.
    /// </summary>
    public virtual void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAt = null;
        MarkAsUpdated();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PERSISTENCE METHODS - EF Core repository ve DbContext için
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Persistence layer için insert öncesi createdAt set etme.
    /// </summary>
    public void SetCreatedAtForPersistence(DateTime createdAt)
    {
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Persistence layer için update öncesi updatedAt set etme.
    /// </summary>
    public void SetUpdatedAtForPersistence(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Persistence layer için soft delete uygulama.
    /// </summary>
    public void SetDeletedForPersistence(DateTime deletedAt)
    {
        IsDeleted = true;
        DeletedAt = deletedAt;
    }
}
