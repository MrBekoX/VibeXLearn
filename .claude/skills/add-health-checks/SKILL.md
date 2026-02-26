---
name: add-health-checks
description: Health check'ler çok basit - sadece static response dönüyor. DB, Redis, Elasticsearch kontrolü yok. Kubernetes/docker orchestration için gerçek sorunlar tespit edilemiyor. Bu skill, production-ready health checks ekler.
---

# Add Production-Ready Health Checks

## Problem

**Risk Level:** MEDIUM

Health endpoints sadece static response dönüyor. Gerçek sorunlar tespit edilemiyor, false positive "healthy" durumları oluşuyor.

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Endpoints/HealthEndpoints.cs`
- `src/Presentation/Platform.WebAPI/Program.cs`

## Solution Steps

### Step 1: Create Custom Health Checks

Create file: `src/Infrastructure/Platform.Infrastructure/HealthChecks/ElasticsearchHealthCheck.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Net.Http;

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
```

Create file: `src/Infrastructure/Platform.Infrastructure/HealthChecks/RedisHealthCheck.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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

            var latency = result.ElapsedMilliseconds;

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
```

### Step 2: Register Health Checks

Modify `src/Presentation/Platform.WebAPI/Program.cs`:

```csharp
// Health checks with tags
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        tags: new[] { "ready", "database" })
    .AddCheck<RedisHealthCheck>(
        name: "redis",
        tags: new[] { "ready", "cache" })
    .AddCheck<ElasticsearchHealthCheck>(
        name: "elasticsearch",
        tags: new[] { "ready", "logging" });

// Add HttpClient for Elasticsearch health check
builder.Services.AddHttpClient("elasticsearch-health");
```

### Step 3: Update Health Endpoints

Modify `src/Presentation/Platform.WebAPI/Endpoints/HealthEndpoints.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Basic liveness - always returns 200 if app is running
        app.MapGet("/health/live", () => Results.Ok(new
        {
            Status = "Alive",
            Timestamp = DateTime.UtcNow
        }))
        .ExcludeFromDescription();

        // Readiness - checks all dependencies
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        })
        .ExcludeFromDescription();

        // Detailed health - all checks including degraded ones
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteHealthCheckResponse
        })
        .ExcludeFromDescription();

        return app;
    }

    private static async Task WriteHealthCheckResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration.TotalMilliseconds,
                Tags = e.Value.Tags,
                Exception = e.Value.Exception?.Message
            }),
            Timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
}
```

### Step 4: Add Health Check Extension Method

Create file: `src/Infrastructure/Platform.Infrastructure/Extensions/HealthCheckExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Platform.Infrastructure.HealthChecks;

namespace Platform.Infrastructure.Extensions;

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration config)
    {
        builder.AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" });

        var elasticUrl = config.GetConnectionString("Elasticsearch");
        if (!string.IsNullOrEmpty(elasticUrl))
        {
            builder.AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: new[] { "ready" });
        }

        return builder;
    }
}
```

### Step 5: Register Health Check Services

Add to `src/Infrastructure/Platform.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`:

```csharp
// Add HTTP client for health checks
services.AddHttpClient("elasticsearch-health");
```

## Usage Examples

### Kubernetes Probes

```yaml
# deployment.yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10
  failureThreshold: 3
```

### Docker Compose Health Check

```yaml
# docker-compose.yml
services:
  api:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

## Verification

```bash
# Test liveness (should always return 200)
curl http://localhost:8080/health/live
# {"status":"Alive","timestamp":"2025-02-25T..."}

# Test readiness (checks DB, Redis, Elasticsearch)
curl http://localhost:8080/health/ready
# {"status":"Healthy","totalDuration":45.2,"checks":[...]}

# Test with Redis down (should return 503)
docker stop vibexlearn-redis
curl http://localhost:8080/health/ready
# {"status":"Unhealthy","checks":[{"name":"redis","status":"Unhealthy",...}]}
```

## Priority

**MEDIUM** - Production monitoring için gerekli.
