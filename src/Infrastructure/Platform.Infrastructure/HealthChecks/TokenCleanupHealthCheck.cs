using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.Domain.Entities;
using Platform.Persistence.Context;

namespace Platform.Infrastructure.HealthChecks;

/// <summary>
/// Health check that monitors expired/revoked token accumulation.
/// Reports Degraded when count exceeds threshold.
/// </summary>
public class TokenCleanupHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    private const int DegradedThreshold = 10_000;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var expiredCount = await dbContext.Set<RefreshToken>()
                .Where(t => t.ExpiresAt < DateTime.UtcNow || t.IsRevoked)
                .CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["expiredTokenCount"] = expiredCount,
                ["threshold"] = DegradedThreshold
            };

            if (expiredCount > DegradedThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"High number of expired tokens: {expiredCount}",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Expired tokens: {expiredCount}",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Token cleanup health check failed",
                ex);
        }
    }
}
