using Microsoft.EntityFrameworkCore.Storage;
using Platform.Application.Common.Interfaces;
using Platform.Persistence.Context;

namespace Platform.Persistence.Repositories;

/// <summary>
/// Unit of Work implementasyonu.
/// </summary>
public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public Task<int> SaveChangesAsync(CancellationToken ct)
        => context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct)
        => _transaction = await context.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        // EF Core 10: simple Func<Task> overload removed; use TState-based signature
        return strategy.ExecuteAsync(
            state: action,
            operation: async (ctx, state, token) =>
            {
                await using var tx = await ctx.Database.BeginTransactionAsync(token);
                try
                {
                    await state();
                    await tx.CommitAsync(token);
                }
                catch
                {
                    await tx.RollbackAsync(token);
                    throw;
                }
                return true;
            },
            verifySucceeded: null,
            cancellationToken: ct);
    }

    public void Dispose() => _transaction?.Dispose();
}
