using Microsoft.EntityFrameworkCore;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Domain.Common;
using Platform.Persistence.Context;

namespace Platform.Persistence.Repositories;

/// <summary>
/// EF Core read repository implementasyonu.
/// </summary>
public sealed class EfReadRepository<T>(AppDbContext context)
    : IReadRepository<T> where T : BaseEntity
{
    private IQueryable<T> Set(bool tracking)
        => tracking
            ? context.Set<T>()
            : context.Set<T>().AsNoTracking();

    private static IQueryable<T> ApplyIncludes(
        IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, object>>[] includes)
        => includes.Aggregate(query, (q, inc) => q.Include(inc));

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct, bool tracking = false)
    {
        if (tracking)
            return await context.Set<T>().FindAsync([id], ct);

        return await context.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<T?> GetAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct,
        bool tracking = false,
        params System.Linq.Expressions.Expression<Func<T, object>>[] includes)
    {
        var query = ApplyIncludes(Set(tracking), includes);
        return await query
            .Where(e => !e.IsDeleted)
            .FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<IList<T>> GetAllAsync(CancellationToken ct, bool tracking = false)
        => await Set(tracking)
            .Where(e => !e.IsDeleted)
            .ToListAsync(ct);

    public async Task<IList<T>> GetListAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct,
        bool tracking = false,
        params System.Linq.Expressions.Expression<Func<T, object>>[] includes)
    {
        var query = ApplyIncludes(Set(tracking), includes);
        return await query
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<T>> GetPagedAsync(
        System.Linq.Expressions.Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        int page,
        int pageSize,
        CancellationToken ct,
        bool tracking = false,
        params System.Linq.Expressions.Expression<Func<T, object>>[] includes)
    {
        var query = ApplyIncludes(Set(tracking), includes)
            .Where(e => !e.IsDeleted);

        if (predicate is not null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(ct);
        var items      = await orderBy(query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, totalCount, page, pageSize);
    }

    public async Task<IList<TProjection>> GetProjectedAsync<TProjection>(
        System.Linq.Expressions.Expression<Func<T, bool>>? predicate,
        System.Linq.Expressions.Expression<Func<T, TProjection>> selector,
        CancellationToken ct)
    {
        var query = context.Set<T>().AsNoTracking().Where(e => !e.IsDeleted);
        if (predicate is not null) query = query.Where(predicate);
        return await query.Select(selector).ToListAsync(ct);
    }

    public Task<bool> AnyAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct)
        => context.Set<T>().AsNoTracking()
            .Where(e => !e.IsDeleted)
            .AnyAsync(predicate, ct);

    public Task<int> CountAsync(
        System.Linq.Expressions.Expression<Func<T, bool>>? predicate, CancellationToken ct)
    {
        var query = context.Set<T>().AsNoTracking().Where(e => !e.IsDeleted);
        return predicate is null
            ? query.CountAsync(ct)
            : query.CountAsync(predicate, ct);
    }

    public IQueryable<T> GetQuery()
        => context.Set<T>().AsNoTracking().Where(e => !e.IsDeleted);

    public IQueryable<T> GetTrackedQuery()
        => context.Set<T>().Where(e => !e.IsDeleted);
}
