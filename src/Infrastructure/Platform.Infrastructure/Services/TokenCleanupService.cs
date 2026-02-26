using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Interfaces;
using Platform.Domain.Entities;
using Platform.Persistence.Context;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Scoped implementation that deletes expired/revoked refresh tokens
/// older than the configured retention period.
/// </summary>
public class TokenCleanupService(
    AppDbContext dbContext,
    ILogger<TokenCleanupService> logger,
    IConfiguration config) : ITokenCleanupService
{
    private readonly int _retentionDays = config.GetValue("TokenCleanup:RetentionDays", 30);

    public async Task<int> CleanupAsync(CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        var deletedCount = await dbContext.Set<RefreshToken>()
            .Where(t => (t.IsRevoked || t.ExpiresAt < DateTime.UtcNow) && t.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("Cleaned up {Count} expired tokens", deletedCount);

        return deletedCount;
    }
}
