using Platform.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Redis-based JWT token blacklist.
/// Uses SET NX EX for atomic operations.
/// </summary>
public sealed class TokenBlacklistService(
    IConnectionMultiplexer redis,
    ILogger<TokenBlacklistService> logger) : ITokenBlacklistService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string KeyPrefix = "blacklist:jti:";

    public async Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = $"{KeyPrefix}{jti}";
        await _db.StringSetAsync(key, "1", ttl);
        logger.LogDebug("Token blacklisted: {Jti}", jti[..8] + "***");
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default)
    {
        var key = $"{KeyPrefix}{jti}";
        return await _db.KeyExistsAsync(key);
    }
}
