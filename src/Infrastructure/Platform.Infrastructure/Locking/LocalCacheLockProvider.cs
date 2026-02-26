using System.Collections.Concurrent;

namespace Platform.Infrastructure.Locking;

/// <summary>
/// In-process lock provider using <see cref="SemaphoreSlim"/>.
/// Includes periodic cleanup to prevent memory leaks from unused semaphores.
/// </summary>
/// <remarks>
/// <para>
/// Memory leak prevention: Uses reference counting and periodic cleanup timer.
/// Semaphores are removed when ref count is 0 and semaphore is available.
/// </para>
/// <para>
/// Thread-safety: All operations are thread-safe via ConcurrentDictionary and atomic operations.
/// </para>
/// </remarks>
public sealed class LocalCacheLockProvider : ICacheLockProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, ReferenceCountedSemaphore> _semaphores = new();
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public LocalCacheLockProvider()
    {
        // Periodic cleanup: remove unused semaphores to prevent memory leaks
        _cleanupTimer = new Timer(CleanupUnusedSemaphores, null, _cleanupInterval, _cleanupInterval);
    }

    public async Task<ILockHandle> AcquireLockAsync(string key, TimeSpan timeout, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var semaphore = _semaphores.GetOrAdd(key, _ => new ReferenceCountedSemaphore());

        // Increment reference count to prevent cleanup while in use
        semaphore.IncrementReference();

        var acquired = await semaphore.Semaphore.WaitAsync(timeout, ct).ConfigureAwait(false);

        if (!acquired)
        {
            // Failed to acquire - decrement reference immediately
            semaphore.DecrementReference();
            return new LockHandle(semaphore, acquired: false, ownsReference: false);
        }

        return new LockHandle(semaphore, acquired: true, ownsReference: true);
    }

    /// <summary>
    /// Cleanup callback: removes semaphores with no references that are available.
    /// </summary>
    private void CleanupUnusedSemaphores(object? state)
    {
        if (_disposed) return;

        foreach (var kvp in _semaphores.ToList())
        {
            // Remove if: no active references AND semaphore is available (not held)
            if (kvp.Value.RefCount == 0 && kvp.Value.Semaphore.CurrentCount == 1)
            {
                // TryRemove is thread-safe
                _semaphores.TryRemove(kvp.Key, out _);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cleanupTimer.Dispose();

        // Dispose all semaphores
        foreach (var kvp in _semaphores)
        {
            kvp.Value.Semaphore.Dispose();
        }

        _semaphores.Clear();
    }

    /// <summary>
    /// A semaphore wrapper with reference counting for cleanup management.
    /// </summary>
    private sealed class ReferenceCountedSemaphore
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int RefCount => _refCount;

        private int _refCount;

        public void IncrementReference() => Interlocked.Increment(ref _refCount);
        public void DecrementReference() => Interlocked.Decrement(ref _refCount);
    }

    /// <summary>
    /// Lock handle that releases the semaphore and decrements reference count on dispose.
    /// </summary>
    private sealed class LockHandle : ILockHandle
    {
        private readonly ReferenceCountedSemaphore _semaphore;
        private readonly bool _ownsReference;
        private bool _disposed;

        public bool IsAcquired { get; }

        public LockHandle(ReferenceCountedSemaphore semaphore, bool acquired, bool ownsReference)
        {
            _semaphore = semaphore;
            IsAcquired = acquired;
            _ownsReference = ownsReference;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;

            if (IsAcquired)
            {
                // Release the semaphore lock
                _semaphore.Semaphore.Release();
            }

            // Only decrement if we still own the reference
            if (_ownsReference)
                _semaphore.DecrementReference();

            return ValueTask.CompletedTask;
        }
    }
}
