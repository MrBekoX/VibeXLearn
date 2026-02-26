using MessagePack;
using MessagePack.Resolvers;

namespace Platform.Infrastructure.Serialization;

/// <summary>
/// MessagePack-based cache serializer.
/// ~2-5x faster than JSON, ~30-50% smaller payload.
/// Uses ContractlessStandardResolver for attribute-free serialization.
/// For maximum performance, annotate DTOs with [MessagePackObject] + [Key(n)].
/// </summary>
public sealed class MessagePackCacheSerializer : ICacheSerializer
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance);

    public string Name => "MessagePack";

    public byte[] Serialize<T>(T value)
    {
        return MessagePackSerializer.Serialize(value, Options);
    }

    public T? Deserialize<T>(byte[] bytes)
    {
        if (bytes.Length == 0)
            return default;

        return MessagePackSerializer.Deserialize<T>(bytes, Options);
    }
}
