namespace Platform.Infrastructure.Locking;

/// <summary>
/// Cache lock abstraction for stampede protection.
/// Supports both local (in-process) and distributed (Redis) locking strategies.
/// </summary>
public interface ICacheLockProvider
{
    /// <summary>
    /// Attempts to acquire a lock for the given cache key.
    /// </summary>
    /// <param name="key">The cache key to lock on.</param>
    /// <param name="timeout">Maximum time to wait for lock acquisition.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A lock handle that should be disposed to release the lock.</returns>
    Task<ILockHandle> AcquireLockAsync(string key, TimeSpan timeout, CancellationToken ct);
}

/// <summary>
/// Represents an acquired lock handle.
/// Must be disposed to release the lock.
/// </summary>
public interface ILockHandle : IAsyncDisposable
{
    /// <summary>
    /// Indicates whether the lock was successfully acquired.
    /// </summary>
    bool IsAcquired { get; }
}
