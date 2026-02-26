# Phase 1: Stampede Protection 🔴

**Hedef:** Concurrent cache miss'lerde tek DB çağrısı garantisi

## 1.1 Yeni Dosyalar

```
src/Infrastructure/Platform.Infrastructure/
├── Locking/
│   ├── ICacheLockProvider.cs           ← Lock abstraction + ILockHandle
│   ├── LocalCacheLockProvider.cs       ← SemaphoreSlim (in-process) + cleanup
│   └── DistributedCacheLockProvider.cs ← Redis SET NX EX + Lua release
```

## 1.2 ICacheLockProvider Interface

```csharp
public interface ICacheLockProvider
{
    Task<ILockHandle> AcquireLockAsync(string key, TimeSpan timeout, CancellationToken ct);
}

public interface ILockHandle : IAsyncDisposable
{
    bool IsAcquired { get; }
}
```

## 1.3 LocalCacheLockProvider (Memory Leak Prevention)

- `ConcurrentDictionary<string, ReferenceCountedSemaphore>` ile key-specific lock
- Periodic cleanup timer (5 dk) — `RefCount == 0 && CurrentCount == 1` olanları temizle
- **Backing field fix:** `Interlocked.Increment(ref _refCount)` — property üzerinde çalışmaz

```csharp
private sealed class ReferenceCountedSemaphore
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    private int _refCount;
    public int RefCount => _refCount;
    public void IncrementReference() => Interlocked.Increment(ref _refCount);
    public void DecrementReference() => Interlocked.Decrement(ref _refCount);
}
```

- **Double-decrement fix:** `LockHandle` constructor'a `ownsReference` parametresi eklenir

```csharp
// AcquireLockAsync — fail path:
if (!acquired)
{
    semaphore.DecrementReference();
    return new LockHandle(semaphore, acquired: false, ownsReference: false);
}
return new LockHandle(semaphore, acquired: true, ownsReference: true);

// LockHandle.DisposeAsync:
if (IsAcquired) _semaphore.Semaphore.Release();
if (_ownsReference) _semaphore.DecrementReference();
```

- `IDisposable` implementasyonu — cleanup timer dispose

## 1.4 DistributedCacheLockProvider

- `Redis SET key value NX EX seconds` (atomic acquire)
- Exponential backoff retry (50ms → 1s max, deadline-based)
- Lua script ile atomic release (ownership verification):

```lua
if redis.call("GET", KEYS[1]) == ARGV[1] then
    return redis.call("DEL", KEYS[1])
else
    return 0
end
```

- Redis fail → graceful degradation (`IsAcquired = false`)

## 1.5 ICacheService — shouldCache Predicate (Failure Caching Prevention)

**Kritik:** Mevcut `GetOrSetAsync` failure response'ları da cache'ler. `shouldCache` predicate eklenmeli:

```csharp
// ICacheService.cs — yeni overload:
Task<T> GetOrSetAsync<T>(
    string key,
    Func<CancellationToken, Task<T>> factory,
    Func<T, bool>? shouldCache,
    TimeSpan? l1Duration,
    TimeSpan? l2Duration,
    CancellationToken ct = default);
```

## 1.6 CacheService.GetOrSetAsync Modification

```csharp
public async Task<T> GetOrSetAsync<T>(...)
{
    // ... existing L1/L2 read ...

    // Lock acquisition
    await using var lockHandle = await _lockProvider.AcquireLockAsync(
        $"lock:{key}", _settings.LockTimeout, ct);

    if (!lockHandle.IsAcquired)
    {
        logger.LogWarning("Lock failed for {Key}. Executing uncached.", key);
        return await factory(ct);
    }

    // Double-check pattern
    var doubleCheck = await GetAsync<T>(key, ct);
    if (doubleCheck is not null) return doubleCheck;

    var result = await factory(ct);

    // Failure guard
    if (shouldCache is not null && !shouldCache(result))
        return result;

    // ... write both layers ...
}
```

## 1.7 QueryCachingBehavior Değişikliği

```csharp
if (cacheableQuery.BypassCache)
{
    var response = await next();
    if (!ResultReflectionHelper.IsFailure(response))
        await cache.SetAsync(key, response, l2, ct);
    return response;
}

return await cache.GetOrSetAsync(
    key,
    async _ => await next(),
    shouldCache: r => !ResultReflectionHelper.IsFailure(r),
    l1Duration: null,
    l2Duration: l2,
    ct);
```

## 1.8 CacheSettings Additions

```csharp
public TimeSpan LockTimeout { get; init; } = TimeSpan.FromSeconds(5);
public TimeSpan DistributedLockTtl { get; init; } = TimeSpan.FromSeconds(30);
public bool EnableDistributedLocking { get; init; } = true;
```

## 1.9 DI Registration

> **Not:** `DistributedCacheLockProvider` constructor'ı `CacheSettings` (unwrapped) alır, `IOptions<>` değil.
> Logger opsiyonel — mevcut implementation logger kullanmıyor (release failure TTL'e bırakılıyor).

```csharp
services.AddSingleton<ICacheLockProvider>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;
    if (settings.EnableDistributedLocking)
        return new DistributedCacheLockProvider(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            settings);
    return new LocalCacheLockProvider();
});
```
