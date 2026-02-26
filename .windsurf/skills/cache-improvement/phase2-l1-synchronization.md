# Phase 2: L1 Synchronization via Pub/Sub 🔴

**Hedef:** Multi-instance L1 cache tutarlılığı

## 2.1 Yeni Dosyalar

```
src/Infrastructure/Platform.Infrastructure/
├── Models/
│   └── CacheInvalidationMessage.cs
├── Services/
│   └── L1InvalidationSubscriber.cs
```

## 2.2 CacheInvalidationMessage

```csharp
public sealed record CacheInvalidationMessage
{
    public required string KeyPattern { get; init; }
    public required string SourceInstance { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}
```

## 2.3 L1InvalidationSubscriber

- `IHostedService` implementasyonu
- `_instanceId = Guid.NewGuid().ToString("N")[..8]`
- **Self-invalidation guard:** `msg.SourceInstance == _instanceId` → skip
- **Reconnection:** `_multiplexer.ConnectionRestored += OnConnectionRestored` → resubscribe
- Channel: `cache:invalidation` (configurable via `CacheSettings.InvalidationChannelName`)

```csharp
public sealed class L1InvalidationSubscriber : IHostedService, IDisposable
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IMemoryCache _memoryCache;
    private readonly CacheSettings _settings;
    private readonly ILogger<L1InvalidationSubscriber> _logger;
    private readonly string _instanceId = Guid.NewGuid().ToString("N")[..8];
    private ISubscriber? _subscriber;

    public async Task StartAsync(CancellationToken ct)
    {
        _subscriber = _multiplexer.GetSubscriber();
        await SubscribeAsync();

        // KRİTİK: Reconnection handling
        _multiplexer.ConnectionRestored += OnConnectionRestored;
    }

    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        _logger.LogInformation("Redis reconnected. Re-subscribing to {Channel}",
            _settings.InvalidationChannelName);
        _ = SubscribeAsync();
    }

    private async Task SubscribeAsync()
    {
        await _subscriber!.SubscribeAsync(
            RedisChannel.Literal(_settings.InvalidationChannelName), OnMessage);
    }

    private void OnMessage(RedisChannel channel, RedisValue message)
    {
        var msg = JsonSerializer.Deserialize<CacheInvalidationMessage>(message!);
        if (msg is null) return;

        // Self-invalidation guard
        if (msg.SourceInstance == _instanceId) return;

        // L1 invalidation via CacheService.InvalidateLocalL1
        // ...
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _multiplexer.ConnectionRestored -= OnConnectionRestored;
        if (_subscriber is not null)
            await _subscriber.UnsubscribeAsync(
                RedisChannel.Literal(_settings.InvalidationChannelName));
    }
}
```

## 2.4 Key Tracking Mimarisi (Çakışma Çözümü)

> **Karar:** Key tracking **tek sahip** olarak `L1InvalidationSubscriber` içinde kalır.
> `CacheService` kendi `_l1Keys` dictionary'si **tutmaz** — bunun yerine
> `L1InvalidationSubscriber.TrackKey()` / `UntrackKey()` / `InvalidateLocalL1()` kullanır.
>
> **Neden:** Subscriber zaten `IHostedService` + `IMemoryCache` erişimine sahip.
> İki ayrı dictionary tutmak sync sorunu yaratır.

### CacheService Entegrasyonu

`CacheService` constructor'a `L1InvalidationSubscriber` inject edilir:

```csharp
public sealed class CacheService(
    IMemoryCache memoryCache,
    IConnectionMultiplexer multiplexer,
    IOptions<CacheSettings> cacheSettingsOptions,
    ICacheLockProvider lockProvider,              // Phase 1
    L1InvalidationSubscriber l1Subscriber,        // Phase 2 — key tracking + pub/sub
    ILogger<CacheService> logger) : ICacheService
```

### L1 Write with Tracking (SetAsync / GetOrSetAsync / GetAsync backfill)

Her `memoryCache.Set()` çağrısından sonra `l1Subscriber.TrackKey(key)` çağrılır:

```csharp
// L1 write — tüm Set noktalarında:
memoryCache.Set(key, value, l1);
_l1Subscriber.TrackKey(key);
```

### L1 Eviction Callback (Opsiyonel — Otomatik Untrack)

```csharp
var entry = memoryCache.CreateEntry(key);
entry.Value = value;
entry.AbsoluteExpirationRelativeToNow = l1;
entry.RegisterPostEvictionCallback((evictedKey, _, reason, _) =>
{
    if (reason != EvictionReason.Replaced)
        _l1Subscriber.UntrackKey(evictedKey.ToString()!);
});
using (entry) { }
_l1Subscriber.TrackKey(key);
```

### Local Invalidation (RemoveByPatternAsync)

```csharp
// CacheService.RemoveByPatternAsync — local L1:
_l1Subscriber.InvalidateLocalL1(pattern);
```

> `InvalidateLocalL1` zaten `L1InvalidationSubscriber` içinde implement edilmiş:
> glob→regex dönüşümü + `_trackedKeys` üzerinde pattern match + `_memoryCache.Remove()`

## 2.5 CacheService.RemoveByPatternAsync Modification

Mevcut SCAN logic'ten sonra Pub/Sub broadcast eklenir:

```csharp
// 1. Local L1 invalidation (delegate to L1InvalidationSubscriber)
_l1Subscriber.InvalidateLocalL1(pattern);

// 2. Redis deletion (existing SCAN logic or tag-based — Phase 4)
// ...

// 3. Broadcast to other instances
if (_settings.EnableL1Synchronization)
{
    var message = new CacheInvalidationMessage
    {
        KeyPattern = pattern,
        SourceInstance = _l1Subscriber.InstanceId,
        CorrelationId = correlationId
    };

    try
    {
        var subscriber = multiplexer.GetSubscriber();
        await subscriber.PublishAsync(
            _settings.InvalidationChannelName,
            JsonSerializer.Serialize(message));
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to broadcast invalidation for {Pattern}", pattern);
    }
}
```

## 2.6 CacheSettings Additions

```csharp
public string InvalidationChannelName { get; init; } = "cache:invalidation";
public bool EnableL1Synchronization { get; init; } = true;
```

## 2.7 DI Registration

```csharp
services.AddHostedService<L1InvalidationSubscriber>();
```
