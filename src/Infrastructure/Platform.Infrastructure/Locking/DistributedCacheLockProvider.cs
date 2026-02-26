using StackExchange.Redis;
using Platform.Application.Common.Models.Cache;

namespace Platform.Infrastructure.Locking;

/// <summary>
/// Distributed lock provider using Redis SET NX EX pattern.
/// Uses Lua script for atomic release to prevent releasing another client's lock.
/// </summary>
/// <remarks>
/// <para>
/// Lock safety: Lock value (GUID) is checked before release to prevent
/// accidentally releasing a lock acquired by another client after expiry.
/// </para>
/// <para>
/// Exponential backoff: Uses increasing delays between retry attempts.
/// </para>
/// </remarks>
public sealed class DistributedCacheLockProvider : ICacheLockProvider
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly CacheSettings _settings;

    /// <summary>
    /// Lua script: Only delete the lock if the value matches (atomic ownership check).
    /// Prevents releasing a lock that was acquired by another client after expiry.
    /// </summary>
    private const string ReleaseScript = """
        if redis.call("GET", KEYS[1]) == ARGV[1] then
            return redis.call("DEL", KEYS[1])
        else
            return 0
        end
        """;

    // Raw Lua script string passed directly to ScriptEvaluateAsync
    // (LuaScript.Prepare is for server-loaded scripts via ScriptEvaluateAsync overload)

    public DistributedCacheLockProvider(
        IConnectionMultiplexer multiplexer,
        CacheSettings settings)
    {
        _multiplexer = multiplexer;
        _settings = settings;
    }

    public async Task<ILockHandle> AcquireLockAsync(string key, TimeSpan timeout, CancellationToken ct)
    {
        var db = _multiplexer.GetDatabase();
        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString(); // Unique ownership token
        var lockTtl = _settings.DistributedLockTtl;

        // Exponential backoff parameters
        var delay = TimeSpan.FromMilliseconds(50);
        var maxDelay = TimeSpan.FromSeconds(1);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            // SET NX EX: Set if Not eXists, with EXpiration
            var acquired = await db.StringSetAsync(
                lockKey,
                lockValue,
                lockTtl,
                When.NotExists).ConfigureAwait(false);

            if (acquired)
            {
                return new DistributedLockHandle(db, lockKey, lockValue, acquired: true);
            }

            // Wait before retry with exponential backoff
            await Task.Delay(delay, ct).ConfigureAwait(false);
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, maxDelay.TotalMilliseconds));
        }

        // Timeout reached without acquiring lock
        return new DistributedLockHandle(db, lockKey, lockValue, acquired: false);
    }

    /// <summary>
    /// Distributed lock handle that uses Lua script for safe release.
    /// </summary>
    private sealed class DistributedLockHandle : ILockHandle
    {
        private readonly IDatabase _db;
        private readonly string _lockKey;
        private readonly string _lockValue;
        private bool _disposed;

        public bool IsAcquired { get; }

        public DistributedLockHandle(
            IDatabase db,
            string lockKey,
            string lockValue,
            bool acquired)
        {
            _db = db;
            _lockKey = lockKey;
            _lockValue = lockValue;
            IsAcquired = acquired;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            if (!IsAcquired) return;

            try
            {
                // Atomic release via Lua script: only delete if value matches
                // This prevents releasing a lock acquired by another client
                var parameters = new
                {
                    lockKey = (RedisKey)_lockKey,
                    lockValue = (RedisValue)_lockValue
                };

                await _db.ScriptEvaluateAsync(
                    ReleaseScript,
                    new RedisKey[] { _lockKey },
                    new RedisValue[] { _lockValue }).ConfigureAwait(false);
            }
            catch
            {
                // Lock will expire automatically via TTL - safe to ignore release failure
                // Log in production if needed
            }
        }
    }
}
