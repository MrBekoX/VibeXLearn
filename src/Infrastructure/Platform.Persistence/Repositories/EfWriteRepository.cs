using Microsoft.EntityFrameworkCore;
using Platform.Application.Common.Interfaces;
using Platform.Domain.Common;
using Platform.Persistence.Context;

namespace Platform.Persistence.Repositories;

/// <summary>
/// EF Core write repository implementasyonu.
/// Audit fields (CreatedAt, UpdatedAt) are set centrally by AppDbContext.SaveChangesAsync.
/// Only SoftDelete needs explicit handling here (unique to this path).
/// </summary>
public sealed class EfWriteRepository<T>(AppDbContext context)
    : IWriteRepository<T> where T : BaseEntity
{
    public async Task AddAsync(T entity, CancellationToken ct)
    {
        await context.Set<T>().AddAsync(entity, ct);
    }

    public Task UpdateAsync(T entity, CancellationToken ct)
    {
        context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(T entity, CancellationToken ct)
    {
        entity.SetDeletedForPersistence(DateTime.UtcNow);
        context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        await context.Set<T>().AddRangeAsync(entities, ct);
    }

    public Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        context.Set<T>().UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task SoftDeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        var list = entities.ToList();
        var now  = DateTime.UtcNow;
        list.ForEach(e => e.SetDeletedForPersistence(now));
        context.Set<T>().UpdateRange(list);
        return Task.CompletedTask;
    }

    public async Task UpsertAsync(T entity, CancellationToken ct)
    {
        var exists = await context.Set<T>()
            .AsNoTracking()
            .AnyAsync(e => e.Id == entity.Id, ct);

        if (exists)
            await UpdateAsync(entity, ct);
        else
            await AddAsync(entity, ct);
    }
}
