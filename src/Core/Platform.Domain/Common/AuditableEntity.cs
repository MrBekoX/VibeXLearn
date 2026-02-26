namespace Platform.Domain.Common;

/// <summary>
/// Kim oluşturdu / kim güncelledi bilgisi gereken entity'ler için.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    /// <summary>
    /// Set who created this entity.
    /// </summary>
    public void SetCreatedBy(Guid userId)
    {
        if (CreatedById.HasValue)
            throw new DomainException("CreatedById is already set.");

        CreatedById = userId;
    }

    /// <summary>
    /// Set who updated this entity.
    /// </summary>
    public void SetUpdatedBy(Guid userId)
    {
        UpdatedById = userId;
        MarkAsUpdated();
    }
}
