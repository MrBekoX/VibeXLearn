using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Domain.Entities;
using Platform.Persistence.Context;

namespace Platform.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired and revoked refresh tokens.
/// Runs every 24 hours by default.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _retentionPeriod;

    public RefreshTokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<RefreshTokenCleanupService> logger,
        IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var hours = config.GetValue("TokenCleanup:IntervalHours", 24);
        _cleanupInterval = TimeSpan.FromHours(hours);

        var days = config.GetValue("TokenCleanup:RetentionDays", 30);
        _retentionPeriod = TimeSpan.FromDays(days);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Refresh Token Cleanup Service started. Interval: {Interval}h, Retention: {Retention}d",
            _cleanupInterval.TotalHours, _retentionPeriod.TotalDays);

        // Initial delay before first cleanup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow - _retentionPeriod;

        _logger.LogInformation(
            "Starting token cleanup. Cutoff date: {Cutoff}",
            cutoffDate);

        var deletedCount = await dbContext.Set<RefreshToken>()
            .Where(t => (t.IsRevoked || t.ExpiresAt < DateTime.UtcNow) && t.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Token cleanup completed. Deleted {Count} expired/revoked tokens",
                deletedCount);
        }
        else
        {
            _logger.LogDebug("Token cleanup completed. No tokens to delete");
        }
    }
}
