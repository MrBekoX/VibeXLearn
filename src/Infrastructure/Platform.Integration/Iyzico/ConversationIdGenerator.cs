using System.Security.Cryptography;

namespace Platform.Integration.Iyzico;

/// <summary>
/// Kriptografik güvenli ConversationId üreticisi.
/// </summary>
public static class ConversationIdGenerator
{
    /// <summary>
    /// Format: {userId_8hex}-{timestamp_ms}-{random_hex_16}
    /// Örnek: "a1b2c3d4-1704067200000-f8e2a91c3d7b4f5e"
    /// </summary>
    public static string Generate(Guid userId)
    {
        var prefix    = userId.ToString("N")[..8];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random    = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        return $"{prefix}-{timestamp}-{random}";
    }
}
