using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Cache;
using Platform.Infrastructure.Locking;
using Platform.Infrastructure.Models;
using Platform.Infrastructure.Serialization;
using Platform.Infrastructure.Services.Tagging;
using StackExchange.Redis;

namespace Platform.Infrastructure.Services;

/// <summary>
/// L1 (Memory) + L2 (Redis) cache implementation.
/// Singleton — IMemoryCache and IConnectionMultiplexer are both thread-safe.
///
/// Integrations:
///   Phase 1 — Stampede protection via <see cref="ICacheLockProvider"/>
///   Phase 2 — L1 key tracking + Pub/Sub broadcast via <see cref="L1InvalidationSubscriber"/>
///   Phase 3 — Pluggable serialization via <see cref="ICacheSerializer"/>
///   Phase 4 — Tag-based invalidation via <see cref="ICacheTagManager"/>
/// </summary>
public sealed class CacheService(
    IMemoryCache memoryCache,
    IConnectionMultiplexer multiplexer,
    IOptions<CacheSettings> cacheSettingsOptions,
    ICacheLockProvider lockProvider,
    L1InvalidationSubscriber l1Subscriber,
    ICacheSerializer serializer,
    ICacheTagManager tagManager,
    CacheMetrics metrics,
    ILogger<CacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CacheSettings _settings = cacheSettingsOptions.Value;
    private readonly IDatabase _redisDb = multiplexer.GetDatabase();

    // ── L1 TTL helper ──────────────────────────────────────────────────────
    private TimeSpan L1Duration(TimeSpan l2) =>
        TimeSpan.FromTicks((long)(l2.Ticks * _settings.L1ToL2Ratio));

    // ── L1 write with key tracking (Phase 2) ─────────────────────────────
    private void SetL1<T>(string key, T value, TimeSpan l1)
    {
        var entry = memoryCache.CreateEntry(key);
        entry.Value = value;
        entry.AbsoluteExpirationRelativeToNow = l1;
        entry.RegisterPostEvictionCallback((evictedKey, _, reason, _) =>
        {
            if (reason != EvictionReason.Replaced)
                l1Subscriber.UntrackKey(evictedKey.ToString()!);
        });
        using (entry) { }
        l1Subscriber.TrackKey(key);
    }

    // ── L2 write with optional tag association (Phase 3 + 4) ─────────────
    private async Task SetL2Async<T>(string key, T value, TimeSpan l2, CancellationToken ct)
    {
        var bytes = serializer.Serialize(value);

        try
        {
            if (_settings.EnableTagBasedInvalidation)
            {
                var tags = ExtractTagsFromKey(key);
                if (tags.Count > 0)
                {
                    await tagManager.AssociateTagsAsync(key, tags, _settings.TagExpiration, ct)
                        .ConfigureAwait(false);
                }
            }

            await _redisDb.StringSetAsync(key, bytes, l2).ConfigureAwait(false);
            logger.LogDebug("Cache SET: {Key} | L2:{L2} | Serializer:{Serializer}", key, l2, serializer.Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis SET failed for key {Key}. L1 written.", key);
        }
    }

    // ── L2 read with migration fallback (Phase 3) ────────────────────────
    private T? DeserializeL2<T>(byte[] bytes)
    {
        try
        {
            return serializer.Deserialize<T>(bytes);
        }
        catch when (_settings.SerializerMode == CacheSerializerMode.JsonReadMessagePackWrite)
        {
            // Fallback: read old JSON-formatted data during migration
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
    }

    // ── Read ───────────────────────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // L1: Memory cache
        if (memoryCache.TryGetValue(key, out T? cached))
        {
            metrics.RecordL1Hit(key);
            logger.LogDebug("Cache HIT (L1): {Key}", key);
            return cached;
        }

        metrics.RecordL1Miss(key);

        // L2: Redis
        RedisValue raw;
        try
        {
            raw = await _redisDb.StringGetAsync(key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis GET failed for key {Key}. Returning cache miss.", key);
            metrics.RecordL2Miss(key);
            return default;
        }

        if (raw.IsNullOrEmpty)
        {
            metrics.RecordL2Miss(key);
            logger.LogDebug("Cache MISS: {Key}", key);
            return default;
        }

        metrics.RecordL2Hit(key);
        var value = DeserializeL2<T>((byte[])raw!);
        if (value is not null)
        {
            // L1 backfill — derive from actual Redis TTL when available
            var keyTtl = await _redisDb.KeyTimeToLiveAsync(key).ConfigureAwait(false);
            var l2ForBackfill = keyTtl.GetValueOrDefault(_settings.DefaultL2Duration);
            var l1Ttl = L1Duration(l2ForBackfill);
            SetL1(key, value, l1Ttl);
            logger.LogDebug("Cache HIT (L2 → L1 backfill): {Key}", key);
        }

        return value;
    }

    // ── Write ──────────────────────────────────────────────────────────────

    public async Task SetAsync<T>(string key, T value, TimeSpan? l2Expiration = null, CancellationToken ct = default)
    {
        var l2 = l2Expiration ?? _settings.DefaultL2Duration;
        var l1 = L1Duration(l2);

        // L1 with tracking
        SetL1(key, value, l1);

        // L2 with tag association
        await SetL2Async(key, value, l2, ct).ConfigureAwait(false);
    }

    // ── Remove ─────────────────────────────────────────────────────────────

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        memoryCache.Remove(key);
        l1Subscriber.UntrackKey(key);

        try
        {
            await _redisDb.KeyDeleteAsync(key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis DELETE failed for key {Key}.", key);
        }

        logger.LogDebug("Cache REMOVE: {Key}", key);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        // 1. Local L1 invalidation (Phase 2)
        l1Subscriber.InvalidateLocalL1(pattern);

        // 2. Redis deletion — tag-based (Phase 4) or SCAN fallback
        var deletedCount = 0;

        try
        {
            if (_settings.EnableTagBasedInvalidation)
            {
                var tag = PatternToTag(pattern);
                var keys = await tagManager.GetKeysByTagAsync(tag, ct).ConfigureAwait(false);

                if (keys.Count > 0)
                {
                    await _redisDb.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray())
                        .ConfigureAwait(false);
                    deletedCount = keys.Count;
                }
                else
                {
                    // Fallback: SCAN (backward compatibility / no tags yet)
                    deletedCount = await RemoveByPatternScanAsync(pattern, ct).ConfigureAwait(false);
                }
            }
            else
            {
                deletedCount = await RemoveByPatternScanAsync(pattern, ct).ConfigureAwait(false);
            }

            var strategy = _settings.EnableTagBasedInvalidation ? "tag" : "scan";
            metrics.RecordInvalidation(pattern, strategy);
            logger.LogInformation(
                "Cache invalidated: {Pattern} — {Count} key(s) removed.",
                pattern, deletedCount);
        }
        catch (Exception ex)
        {
            // Stale cache is preferable to a 500 error propagating to the client
            logger.LogWarning(ex,
                "RemoveByPatternAsync failed for pattern {Pattern}. Cache may be stale.", pattern);
        }

        // 3. Broadcast to other instances (Phase 2)
        if (_settings.EnableL1Synchronization)
        {
            var message = new CacheInvalidationMessage
            {
                KeyPattern = pattern,
                SourceInstance = l1Subscriber.InstanceId
            };

            try
            {
                var subscriber = multiplexer.GetSubscriber();
                await subscriber.PublishAsync(
                    RedisChannel.Literal(_settings.InvalidationChannelName),
                    JsonSerializer.Serialize(message)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to broadcast invalidation for {Pattern}", pattern);
            }
        }
    }

    // ── SCAN fallback ────────────────────────────────────────────────────
    private async Task<int> RemoveByPatternScanAsync(string pattern, CancellationToken ct)
    {
        var deletedCount = 0;

        foreach (var endpoint in multiplexer.GetEndPoints())
        {
            var server = multiplexer.GetServer(endpoint);
            if (server.IsReplica) continue;

            var redisKeys = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(_redisDb.Database, pattern: pattern))
            {
                redisKeys.Add(key);
            }

            if (redisKeys.Count == 0)
                continue;

            deletedCount += (int)await _redisDb.KeyDeleteAsync(redisKeys.ToArray()).ConfigureAwait(false);
        }

        return deletedCount;
    }

    // ── Exists ─────────────────────────────────────────────────────────────

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (memoryCache.TryGetValue(key, out _)) return true;
        try
        {
            return await _redisDb.KeyExistsAsync(key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis EXISTS failed for key {Key}.", key);
            return false;
        }
    }

    // ── GetOrSet (original overload — delegates to shouldCache overload) ──

    public Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? l1Duration,
        TimeSpan? l2Duration,
        CancellationToken ct = default)
    {
        return GetOrSetAsync(key, factory, shouldCache: null, l1Duration, l2Duration, ct);
    }

    // ── GetOrSet with stampede protection (Phase 1) ──────────────────────

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        Func<T, bool>? shouldCache,
        TimeSpan? l1Duration,
        TimeSpan? l2Duration,
        CancellationToken ct = default)
    {
        var l2 = l2Duration ?? _settings.DefaultL2Duration;
        var l1 = l1Duration ?? L1Duration(l2);

        // L1
        if (memoryCache.TryGetValue(key, out T? cached) && cached is not null)
        {
            metrics.RecordL1Hit(key);
            logger.LogDebug("Cache HIT (L1): {Key}", key);
            return cached;
        }

        metrics.RecordL1Miss(key);

        // L2
        RedisValue raw = default;
        try
        {
            raw = await _redisDb.StringGetAsync(key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis GET failed for key {Key}. Falling through to DB.", key);
        }

        if (!raw.IsNullOrEmpty)
        {
            var fromRedis = DeserializeL2<T>((byte[])raw!);
            if (fromRedis is not null)
            {
                metrics.RecordL2Hit(key);
                SetL1(key, fromRedis, l1);
                logger.LogDebug("Cache HIT (L2 → L1 backfill): {Key}", key);
                return fromRedis;
            }
        }
        else
        {
            metrics.RecordL2Miss(key);
        }

        // ── Stampede protection: lock + double-check (Phase 1) ────────────
        await using var lockHandle = await lockProvider.AcquireLockAsync(
            key, _settings.LockTimeout, ct).ConfigureAwait(false);

        if (!lockHandle.IsAcquired)
        {
            metrics.RecordLockTimeout(key);
            // Lock timeout — execute factory without caching to avoid blocking
            logger.LogWarning("Lock acquisition failed for {Key}. Executing uncached.", key);
            return await factory(ct).ConfigureAwait(false);
        }

        metrics.RecordLockAcquired(key);

        // Double-check: another thread may have populated cache while we waited
        var doubleCheck = await GetAsync<T>(key, ct).ConfigureAwait(false);
        if (doubleCheck is not null) return doubleCheck;

        // DB / factory
        logger.LogDebug("Cache MISS: {Key}", key);
        var result = await factory(ct).ConfigureAwait(false);

        // Failure guard: don't cache if shouldCache returns false
        if (shouldCache is not null && !shouldCache(result))
            return result;

        // Write both layers
        SetL1(key, result, l1);
        await SetL2Async(key, result, l2, ct).ConfigureAwait(false);

        return result;
    }

    // ── Tag helpers (Phase 4) ────────────────────────────────────────────

    /// <summary>
    /// Extracts tags from a cache key based on colon-separated segments.
    /// "courses:list:p1:s20" → ["courses", "courses:list"]
    /// </summary>
    private static IReadOnlyList<string> ExtractTagsFromKey(string key)
    {
        var parts = key.Split(':');
        var tags = new List<string>();

        if (parts.Length >= 1)
            tags.Add(parts[0]);
        if (parts.Length >= 2)
            tags.Add($"{parts[0]}:{parts[1]}");

        return tags;
    }

    /// <summary>
    /// Converts a glob pattern to a tag name.
    /// "courses:*" → "courses"
    /// "courses:list:*" → "courses:list"
    /// </summary>
    private static string PatternToTag(string pattern)
    {
        return pattern.TrimEnd('*').TrimEnd(':');
    }
}
