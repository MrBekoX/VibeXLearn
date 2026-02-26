using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Platform.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Redis connectivity.
/// </summary>
public class RedisHealthCheck(
    IConnectionMultiplexer redis,
    ILogger<RedisHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var result = await db.PingAsync();

            var latency = (long)result.TotalMilliseconds;

            return latency switch
            {
                < 10 => HealthCheckResult.Healthy($"Redis responding in {latency}ms"),
                < 100 => HealthCheckResult.Degraded($"Redis latency high: {latency}ms"),
                _ => HealthCheckResult.Unhealthy($"Redis latency critical: {latency}ms")
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy($"Redis unreachable: {ex.Message}");
        }
    }
}
