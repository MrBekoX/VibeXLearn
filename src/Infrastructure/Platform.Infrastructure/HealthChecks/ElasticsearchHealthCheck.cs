using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    private readonly string? _elasticUsername = config["Elastic:Username"];
    private readonly string? _elasticPassword = config["Elastic:Password"];

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("elasticsearch-health");
            client.Timeout = TimeSpan.FromSeconds(5);
            if (!string.IsNullOrWhiteSpace(_elasticPassword))
            {
                var username = string.IsNullOrWhiteSpace(_elasticUsername)
                    ? "elastic"
                    : _elasticUsername;
                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{username}:{_elasticPassword}"));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);
            }

            var response = await client.GetAsync($"{_elasticUrl}/_cluster/health", ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(content);
                var status = doc.RootElement.TryGetProperty("status", out var statusProp)
                    ? statusProp.GetString()
                    : null;

                return status switch
                {
                    "green" or "yellow" => HealthCheckResult.Healthy("Elasticsearch cluster is healthy"),
                    "red" => HealthCheckResult.Degraded("Elasticsearch cluster status is red"),
                    _ => HealthCheckResult.Degraded($"Unexpected cluster status: {status}")
                };
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
