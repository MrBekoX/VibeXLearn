namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Service for cleaning up expired tokens.
/// </summary>
public interface ITokenCleanupService
{
    /// <summary>
    /// Removes expired and revoked tokens older than retention period.
    /// </summary>
    Task<int> CleanupAsync(CancellationToken ct = default);
}
