using Platform.Domain.Common;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Yazma odaklı repository kontratı.
/// </summary>
public interface IWriteRepository<T> where T : BaseEntity
{
    Task AddAsync(T entity, CancellationToken ct);
    Task UpdateAsync(T entity, CancellationToken ct);

    /// <summary>
    /// Soft delete. <c>IsDeleted = true</c>, <c>DeletedAt = UtcNow</c> set eder.
    /// </summary>
    Task SoftDeleteAsync(T entity, CancellationToken ct);

    /// <summary>EF Core bulk insert.</summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct);

    /// <summary>Birden fazla entity güncellemek için.</summary>
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct);

    Task SoftDeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct);

    /// <summary>
    /// Var → Update, Yok → Insert. Idempotent işlemler için.
    /// </summary>
    Task UpsertAsync(T entity, CancellationToken ct);
}
