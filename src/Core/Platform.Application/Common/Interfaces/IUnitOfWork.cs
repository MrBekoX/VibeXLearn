namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Unit of Work pattern interface.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int>  SaveChangesAsync(CancellationToken ct);

    /// <summary>Birden fazla aggregate'i atomik yazmak için explicit transaction.</summary>
    Task       BeginTransactionAsync(CancellationToken ct);
    Task       CommitAsync(CancellationToken ct);
    Task       RollbackAsync(CancellationToken ct);

    /// <summary>
    /// Execution strategy ile retry destekli transaction bloğu.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct);
}
