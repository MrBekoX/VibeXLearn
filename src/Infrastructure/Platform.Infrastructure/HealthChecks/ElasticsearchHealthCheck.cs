using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Elasticsearch connectivity.
/// </summary>
public class ElasticsearchHealthCheck(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<ElasticsearchHealthCheck> logger) : IHealthCheck
{
    private readonly string _elasticUrl = config.GetConnectionString("Elasticsearch")
        ?? "http://localhost:9200";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync($"{_elasticUrl}/_cluster/health", ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                // Check cluster status
                if (content.Contains("\"status\":\"green\"") ||
                    content.Contains("\"status\":\"yellow\""))
                {
                    return HealthCheckResult.Healthy("Elasticsearch cluster is healthy");
                }

                return HealthCheckResult.Degraded("Elasticsearch cluster status is red");
            }

            return HealthCheckResult.Unhealthy($"Elasticsearch returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Elasticsearch health check failed");
            return HealthCheckResult.Unhealthy($"Elasticsearch unreachable: {ex.Message}");
        }
    }
}
