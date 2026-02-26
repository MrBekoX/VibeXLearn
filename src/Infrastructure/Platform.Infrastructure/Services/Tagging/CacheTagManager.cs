using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace Platform.Infrastructure.Services.Tagging;

/// <summary>
/// Redis-based cache tag manager for efficient bulk invalidation.
/// Uses Redis SETs to maintain tag → keys mappings.
/// </summary>
public sealed class CacheTagManager : ICacheTagManager
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<CacheTagManager> _logger;
    private readonly IDatabase _db;

    private const string TagPrefix = "cache:tag:";

    /// <summary>
    /// Lua script: Atomically SET key + SADD tag + EXPIRE tag.
    /// Ensures key and tag association are written together.
    /// </summary>
    private const string SetWithTagScript = """
        local key = KEYS[1]
        local value = ARGV[1]
        local ttl = tonumber(ARGV[2])
        local tagKey = ARGV[3]
        local tagTtl = tonumber(ARGV[4])

        redis.call('SET', key, value, 'EX', ttl)
        redis.call('SADD', tagKey, key)
        redis.call('EXPIRE', tagKey, tagTtl)
        return 1
        """;

    public CacheTagManager(
        IConnectionMultiplexer multiplexer,
        ILogger<CacheTagManager> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
        _db = multiplexer.GetDatabase();
    }

    public async Task AssociateTagsAsync(
        string key,
        IEnumerable<string> tags,
        TimeSpan tagTtl,
        CancellationToken ct = default)
    {
        var tagList = tags.ToList();
        if (tagList.Count == 0) return;

        var tasks = tagList.Select(async tag =>
        {
            var tagKey = $"{TagPrefix}{tag}";
            await Task.WhenAll(
                _db.SetAddAsync(tagKey, key),
                _db.KeyExpireAsync(tagKey, tagTtl) // Refresh tag TTL
            ).ConfigureAwait(false);
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        _logger.LogDebug(
            "Associated key {Key} with {Count} tags",
            key, tagList.Count);
    }

    public async Task<IReadOnlyList<string>> GetKeysByTagAsync(
        string tag,
        CancellationToken ct = default)
    {
        var tagKey = $"{TagPrefix}{tag}";
        var members = await _db.SetMembersAsync(tagKey).ConfigureAwait(false);
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task<int> CleanupOrphanKeysAsync(
        IEnumerable<string> keys,
        string tag,
        CancellationToken ct = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0) return 0;

        var tagKey = $"{TagPrefix}{tag}";
        var orphansRemoved = 0;

        // Batch EXISTS check (100 keys per batch)
        foreach (var batch in keyList.Chunk(100))
        {
            var tasks = batch.Select(k => _db.KeyExistsAsync(k));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var orphansInBatch = new List<RedisValue>();
            for (var i = 0; i < batch.Length; i++)
            {
                if (!results[i])
                {
                    orphansInBatch.Add(batch[i]);
                }
            }

            if (orphansInBatch.Count > 0)
            {
                await _db.SetRemoveAsync(tagKey, orphansInBatch.ToArray()).ConfigureAwait(false);
                orphansRemoved += orphansInBatch.Count;
            }
        }

        if (orphansRemoved > 0)
        {
            _logger.LogDebug(
                "Cleaned up {Count} orphan keys from tag {Tag}",
                orphansRemoved, tag);
        }

        return orphansRemoved;
    }

    public async Task RemoveKeyFromTagsAsync(
        string key,
        IEnumerable<string> tags,
        CancellationToken ct = default)
    {
        var tagList = tags.ToList();
        if (tagList.Count == 0) return;

        var tasks = tagList.Select(tag =>
        {
            var tagKey = $"{TagPrefix}{tag}";
            return _db.SetRemoveAsync(tagKey, key);
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Atomically sets a cache key and associates it with a tag.
    /// Uses Lua script for atomicity.
    /// </summary>
    public async Task<bool> SetWithTagAsync(
        string key,
        byte[] value,
        TimeSpan keyTtl,
        string tag,
        TimeSpan tagTtl,
        CancellationToken ct = default)
    {
        try
        {
            var tagKey = $"{TagPrefix}{tag}";

            var result = await _db.ScriptEvaluateAsync(
                SetWithTagScript,
                new RedisKey[] { key },
                new RedisValue[]
                {
                    value,
                    (long)keyTtl.TotalSeconds,
                    tagKey,
                    (long)tagTtl.TotalSeconds
                }
            ).ConfigureAwait(false);

            return result.IsNull ? false : (int)result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to set key {Key} with tag {Tag} atomically",
                key, tag);
            return false;
        }
    }
}
