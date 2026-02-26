namespace Platform.Application.Common.Interfaces;

/// <summary>
/// JWT token blacklist service for immediate revocation.
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token's JTI to the blacklist.
    /// </summary>
    Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Checks if a token's JTI is blacklisted.
    /// </summary>
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default);
}
