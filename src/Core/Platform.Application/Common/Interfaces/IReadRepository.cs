using Platform.Domain.Common;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Okuma odaklı repository kontratı.
/// <para>
/// <c>tracking</c> parametresi varsayılan olarak <c>false</c>'tur (AsNoTracking).
/// </para>
/// </summary>
public interface IReadRepository<T> where T : BaseEntity
{
    /// <summary>PK ile getirir. Bulunamazsa <c>null</c> döner.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct, bool tracking = false);

    /// <summary>
    /// Birden fazla Include/filter içeren sorgular için.
    /// </summary>
    Task<T?> GetAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct,
        bool tracking = false,
        params System.Linq.Expressions.Expression<Func<T, object>>[] includes);

    /// <summary>Tüm kayıtları getirir (soft-deleted hariç).</summary>
    Task<IList<T>> GetAllAsync(CancellationToken ct, bool tracking = false);

    /// <summary>Filtrelenmiş liste.</summary>
    Task<IList<T>> GetListAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct,
        bool tracking = false,
        params System.Linq.Expressions.Expression<Func<T, object>>[] includes);

    /// <summary>Sayfalama ile sonuç.</summary>
    Task<Models.Pagination.PagedResult<T>> GetPagedAsync(
        System.Linq.Expressions.Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        int page,
        int pageSize,
        CancellationToken ct,
        bool tracking = false,
        params System.Linq.Expressions.Expression<Func<T, object>>[] includes);

    /// <summary>DTO/projeksiyon sorguları için.</summary>
    Task<IList<TProjection>> GetProjectedAsync<TProjection>(
        System.Linq.Expressions.Expression<Func<T, bool>>? predicate,
        System.Linq.Expressions.Expression<Func<T, TProjection>> selector,
        CancellationToken ct);

    Task<bool>  AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct);
    Task<int>   CountAsync(System.Linq.Expressions.Expression<Func<T, bool>>? predicate, CancellationToken ct);

    /// <summary>AsNoTracking uygulanmış query.</summary>
    IQueryable<T> GetQuery();

    /// <summary>Tracking açık query — sadece change detection gereken senaryolarda.</summary>
    IQueryable<T> GetTrackedQuery();
}
