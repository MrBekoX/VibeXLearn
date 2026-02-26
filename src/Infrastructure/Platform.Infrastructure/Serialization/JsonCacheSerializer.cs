using System.Text.Json;

namespace Platform.Infrastructure.Serialization;

/// <summary>
/// JSON-based cache serializer using System.Text.Json.
/// Default serializer for backward compatibility.
/// </summary>
public sealed class JsonCacheSerializer : ICacheSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string Name => "Json";

    public byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, Options);
    }

    public T? Deserialize<T>(byte[] bytes)
    {
        if (bytes.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(bytes, Options);
    }
}
