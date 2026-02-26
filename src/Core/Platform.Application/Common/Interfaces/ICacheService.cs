namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Cache service interface — L1 (Memory) + L2 (Redis) layered strategy.
/// </summary>
public interface ICacheService
{
    /// <summary>Reads from L1, then L2. Returns default if both miss.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>Writes to both L1 and L2. L1 TTL = L2 × L1ToL2Ratio.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? l2Expiration = null, CancellationToken ct = default);

    /// <summary>Removes a single key from both L1 and L2.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Removes all keys matching the given glob pattern from L1 and L2.
    /// Uses Redis SCAN for safe, iterative key discovery.
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Returns the cached value if present; otherwise executes <paramref name="factory"/>,
    /// stores the result in both L1 and L2, and returns it.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? l1Duration,
        TimeSpan? l2Duration,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the cached value if present; otherwise acquires a lock, executes <paramref name="factory"/>,
    /// and stores the result only if <paramref name="shouldCache"/> returns true.
    /// Provides stampede protection via lock-based concurrency control.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        Func<T, bool>? shouldCache,
        TimeSpan? l1Duration,
        TimeSpan? l2Duration,
        CancellationToken ct = default);
}
