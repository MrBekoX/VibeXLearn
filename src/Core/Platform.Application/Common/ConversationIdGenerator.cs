using System.Security.Cryptography;

namespace Platform.Application.Common;

/// <summary>
/// Kriptografik güvenli ConversationId üreticisi.
/// Format: {userId_8hex}-{timestamp_ms}-{random_hex_16}
/// </summary>
public static class ConversationIdGenerator
{
    public static string Generate(Guid userId)
    {
        var prefix    = userId.ToString("N")[..8];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random    = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        return $"{prefix}-{timestamp}-{random}";
    }
}
