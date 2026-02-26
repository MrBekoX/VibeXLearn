# Phase 4: Key Tagging for Efficient Invalidation 🟡

**Hedef:** SCAN yerine O(N) tag-based invalidation

## 4.1 Yeni Dosyalar

```
src/Infrastructure/Platform.Infrastructure/
├── Services/
│   └── CacheTagManager.cs
```

## 4.2 ICacheTagManager Interface

```csharp
public interface ICacheTagManager
{
    Task AssociateTagsAsync(string key, IEnumerable<string> tags, TimeSpan tagTtl, CancellationToken ct);
    Task<IReadOnlyList<string>> GetKeysByTagAsync(string tag, CancellationToken ct);
    Task CleanupOrphanKeysAsync(string tag, CancellationToken ct);
}
```

## 4.3 Redis Structure

- Tag SET: `cache:tag:{tag}` → `SADD key`
- Örnek: `courses:list:p1:s20` → tags: `["courses", "courses:lists"]`
- Invalidation: `SMEMBERS cache:tag:courses` → `DEL keys`

## 4.4 Atomicity — Lua Script

Key write + tag association atomic yapılır:

```lua
local key = KEYS[1]
local value = ARGV[1]
local ttl = tonumber(ARGV[2])
local tagKey = ARGV[3]

redis.call('SET', key, value, 'EX', ttl)
redis.call('SADD', tagKey, key)
redis.call('EXPIRE', tagKey, 86400)
return 1
```

Birden fazla tag için genişletilmiş versiyon:

```lua
local key = KEYS[1]
local value = ARGV[1]
local ttl = tonumber(ARGV[2])
local tagTtl = tonumber(ARGV[3])
local tagCount = tonumber(ARGV[4])

redis.call('SET', key, value, 'EX', ttl)

for i = 1, tagCount do
    local tagKey = ARGV[4 + i]
    redis.call('SADD', tagKey, key)
    redis.call('EXPIRE', tagKey, tagTtl)
end

return 1
```

## 4.5 CacheTagManager Implementation

```csharp
public sealed class CacheTagManager : ICacheTagManager
{
    private readonly IConnectionMultiplexer _multiplexer;
    private const string TagPrefix = "cache:tag:";

    public async Task AssociateTagsAsync(
        string key, IEnumerable<string> tags, TimeSpan tagTtl, CancellationToken ct)
    {
        var db = _multiplexer.GetDatabase();
        var tasks = tags.Select(tag =>
        {
            var tagKey = $"{TagPrefix}{tag}";
            return Task.WhenAll(
                db.SetAddAsync(tagKey, key),
                db.KeyExpireAsync(tagKey, tagTtl)
            );
        });
        await Task.WhenAll(tasks);
    }

    public async Task<IReadOnlyList<string>> GetKeysByTagAsync(
        string tag, CancellationToken ct)
    {
        var db = _multiplexer.GetDatabase();
        var members = await db.SetMembersAsync($"{TagPrefix}{tag}");
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task CleanupOrphanKeysAsync(string tag, CancellationToken ct)
    {
        var db = _multiplexer.GetDatabase();
        var tagKey = $"{TagPrefix}{tag}";
        var members = await db.SetMembersAsync(tagKey);

        foreach (var batch in members.Chunk(100))
        {
            var tasks = batch.Select(m => db.KeyExistsAsync(m.ToString()));
            var results = await Task.WhenAll(tasks);

            for (int i = 0; i < batch.Length; i++)
            {
                if (!results[i])
                    await db.SetRemoveAsync(tagKey, batch[i]);
            }
        }
    }
}
```

## 4.6 Orphan Cleanup — Background (Not Inline)

Invalidation path'te cleanup yapılmaz (latency). Bunun yerine:
- Background timer (her 5 dk) veya lazy cleanup (her N. çağrıda)
- `SMEMBERS` → batch `EXISTS` check → `SREM` non-existing keys

## 4.7 Tag Extraction

```csharp
// "courses:list:p1:s20" → ["courses", "courses:list"]
// "categories:tree:v1"  → ["categories", "categories:tree"]
private static IReadOnlyList<string> ExtractTagsFromKey(string key)
{
    var parts = key.Split(':');
    var tags = new List<string>();

    if (parts.Length >= 1)
        tags.Add(parts[0]);                          // "courses"
    if (parts.Length >= 2)
        tags.Add($"{parts[0]}:{parts[1]}");          // "courses:list"

    return tags;
}

// "courses:*" → "courses"
// "courses:list:*" → "courses:list"
private static string PatternToTag(string pattern)
{
    return pattern.TrimEnd('*').TrimEnd(':');
}
```

## 4.8 CacheService.RemoveByPatternAsync Modification

```csharp
if (_settings.EnableTagBasedInvalidation)
{
    var tag = PatternToTag(pattern);
    var keys = await _tagManager.GetKeysByTagAsync(tag, ct);

    if (keys.Count > 0)
        await _redisDb.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());
    else
        await RemoveByPatternScanFallbackAsync(pattern, ct); // Backward compat
}
else
{
    await RemoveByPatternScanFallbackAsync(pattern, ct);
}
```

## 4.9 CacheSettings Additions

```csharp
public bool EnableTagBasedInvalidation { get; init; } = true;
public TimeSpan TagExpiration { get; init; } = TimeSpan.FromHours(24);
```

## 4.10 DI Registration

```csharp
services.AddSingleton<ICacheTagManager, CacheTagManager>();
```
