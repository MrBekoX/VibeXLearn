namespace Platform.Infrastructure.Serialization;

/// <summary>
/// Cache serialization abstraction.
/// Supports pluggable serialization strategies (JSON, MessagePack, etc.).
/// </summary>
public interface ICacheSerializer
{
    /// <summary>
    /// Serializes the value to a byte array.
    /// </summary>
    byte[] Serialize<T>(T value);

    /// <summary>
    /// Deserializes the byte array to the specified type.
    /// </summary>
    T? Deserialize<T>(byte[] bytes);

    /// <summary>
    /// Name of the serializer (e.g., "Json", "MessagePack").
    /// Used for logging and diagnostics.
    /// </summary>
    string Name { get; }
}
