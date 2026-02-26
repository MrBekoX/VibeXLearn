# Phase 3: MessagePack Serialization 🟡

**Hedef:** ~2-5x daha hızlı serialization, ~30-50% daha küçük payload

## 3.1 NuGet Packages

```xml
<PackageReference Include="MessagePack" Version="3.1.3" />
<PackageReference Include="MessagePackAnalyzer" Version="3.1.3" />
```

> ⚠️ MessagePack 3.x API'si 2.x'ten farklı. Implementation sırasında doğrulanacak.

## 3.2 Yeni Dosyalar

```
src/Infrastructure/Platform.Infrastructure/
├── Serialization/
│   ├── ICacheSerializer.cs
│   ├── JsonCacheSerializer.cs
│   └── MessagePackCacheSerializer.cs
```

## 3.3 ICacheSerializer Interface

```csharp
public interface ICacheSerializer
{
    byte[] Serialize<T>(T value);
    T? Deserialize<T>(byte[] bytes);
    string Name { get; }
}
```

## 3.4 JsonCacheSerializer

```csharp
public sealed class JsonCacheSerializer : ICacheSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Name => "JSON";

    public byte[] Serialize<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, Options);

    public T? Deserialize<T>(byte[] bytes) =>
        JsonSerializer.Deserialize<T>(bytes, Options);
}
```

## 3.5 MessagePackCacheSerializer

```csharp
public sealed class MessagePackCacheSerializer : ICacheSerializer
{
    public string Name => "MessagePack";

    // MessagePack 3.x API (verify before use)
    public byte[] Serialize<T>(T value) =>
        MessagePackSerializer.Serialize(value, MessagePackSerializerOptions.Standard);

    public T? Deserialize<T>(byte[] bytes) =>
        MessagePackSerializer.Deserialize<T>(bytes, MessagePackSerializerOptions.Standard);
}
```

## 3.6 Strateji

**Başlangıç:** `ContractlessStandardResolver` (~1.5x gain, attribute gerekmez)
**Sonra:** Kritik DTO'lara `[MessagePackObject]` + `[Key(n)]` eklenerek tam performans

## 3.7 Migration Stratejisi (Zero-Downtime)

```csharp
public enum CacheSerializerMode
{
    JsonOnly,                    // Mevcut davranış (default)
    JsonReadMessagePackWrite,    // Yeni yazma MP, okuma JSON fallback
    MessagePackOnly              // TTL expiry sonrası tam geçiş
}
```

Deploy sırası:
1. `JsonOnly` → Mevcut davranış (deploy)
2. `JsonReadMessagePackWrite` → Yeni yazma MessagePack, okuma fallback
3. TTL expiry sonrası `MessagePackOnly` → Tam geçiş

## 3.8 CacheService Entegrasyonu

- Constructor'a `ICacheSerializer` injection
- `SetAsync` → `_serializer.Serialize(value)`
- `GetAsync` → `_serializer.Deserialize<T>(bytes)` + migration mode'da JSON fallback:

```csharp
// GetAsync — migration mode:
try
{
    return _serializer.Deserialize<T>(bytes);
}
catch when (_settings.SerializerMode == CacheSerializerMode.JsonReadMessagePackWrite)
{
    // Fallback: eski JSON formatındaki veriyi oku
    return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
}
```

## 3.9 CacheSettings Addition

```csharp
public CacheSerializerMode SerializerMode { get; init; } = CacheSerializerMode.JsonOnly;
```

## 3.10 DTO Attribute Example (Opsiyonel — Tam Performans İçin)

```csharp
[MessagePackObject]
public sealed record GetAllCoursesQueryDto
{
    [Key(0)] public Guid Id { get; init; }
    [Key(1)] public string Title { get; init; } = default!;
    [Key(2)] public string? Description { get; init; }
    [Key(3)] public decimal Price { get; init; }
}
```
