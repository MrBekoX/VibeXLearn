---
name: cache-improvement
description: L1 (Memory) + L2 (Redis) cache mekanizmasını iyileştirme planı — Stampede Protection, L1 Pub/Sub Sync, MessagePack Serialization, Key Tagging
---

# Cache Mekanizması İyileştirme Planı (Final v3)

L1 (Memory) + L2 (Redis) caching mekanizmasındaki 4 kritik sorunu 4 bağımsız phase ile çözen implementasyon planı.

## Sorunlar

| Öncelik | Sorun | Etki |
|---------|-------|------|
| 🔴 Yüksek | Stampede Protection | Concurrent cache miss'lerde DB flood |
| 🔴 Yüksek | L1 Inconsistency | Multi-instance'da stale L1 cache |
| 🟡 Orta | Serialization Overhead | JSON serialization performans kaybı |
| 🟡 Orta | SCAN Performance | Büyük key space'lerde yavaş invalidation |

## Phases

Her phase bağımsız deploy edilebilir ve geri alınabilir. Detaylar supporting dosyalarda:

1. **[Phase 1: Stampede Protection](phase1-stampede-protection.md)** 🔴 — Lock-based concurrent cache miss prevention
2. **[Phase 2: L1 Synchronization](phase2-l1-synchronization.md)** 🔴 — Redis Pub/Sub ile multi-instance L1 tutarlılığı
3. **[Phase 3: MessagePack Serialization](phase3-messagepack.md)** 🟡 — ~2-5x hızlı serialization, ~30-50% küçük payload
4. **[Phase 4: Key Tagging](phase4-key-tagging.md)** 🟡 — SCAN yerine O(N) tag-based invalidation

## Dosya Özeti

### Yeni Dosyalar (9)

| Dosya | Phase |
|-------|-------|
| `Locking/ICacheLockProvider.cs` | 1 |
| `Locking/LocalCacheLockProvider.cs` | 1 |
| `Locking/DistributedCacheLockProvider.cs` | 1 |
| `Serialization/ICacheSerializer.cs` | 3 |
| `Serialization/JsonCacheSerializer.cs` | 3 |
| `Serialization/MessagePackCacheSerializer.cs` | 3 |
| `Models/CacheInvalidationMessage.cs` | 2 |
| `Services/L1InvalidationSubscriber.cs` | 2 |
| `Services/CacheTagManager.cs` | 4 |

> Tüm yeni dosyalar `src/Infrastructure/Platform.Infrastructure/` altında.

### Değişen Dosyalar (6)

| Dosya | Değişiklik |
|-------|-----------|
| `CacheSettings.cs` | 8 yeni property + 1 enum |
| `ICacheService.cs` | `shouldCache` predicate'li overload |
| `CacheService.cs` | Lock, key tracking, pub/sub, serializer, tag entegrasyonu |
| `QueryCachingBehavior.cs` | `GetOrSetAsync` + failure guard |
| `InfrastructureServiceExtensions.cs` | 4 yeni DI kaydı |
| `Platform.Infrastructure.csproj` | MessagePack NuGet |

## Configuration Example

```json
{
  "Cache": {
    "DefaultL1Duration": "00:02:00",
    "DefaultL2Duration": "00:30:00",
    "L1ToL2Ratio": 0.2,

    "LockTimeout": "00:00:05",
    "DistributedLockTtl": "00:00:30",
    "EnableDistributedLocking": true,

    "InvalidationChannelName": "cache:invalidation",
    "EnableL1Synchronization": true,

    "SerializerMode": "MessagePackOnly",

    "EnableTagBasedInvalidation": true,
    "TagExpiration": "24:00:00"
  }
}
```

## Rollout Order

1. **Phase 1:** Stampede Protection (local + distributed lock + QueryCachingBehavior geçişi)
2. **Phase 2:** L1 Synchronization (Pub/Sub + key tracking + reconnect)
3. **Phase 3:** MessagePack Serialization (contractless → attribute-based)
4. **Phase 4:** Key Tagging (Lua atomic + background orphan cleanup)
