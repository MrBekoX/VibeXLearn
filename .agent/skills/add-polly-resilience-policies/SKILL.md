---
name: add-polly-resilience-policies
description: Polly package referansı var ama kullanılmıyor. HttpClient retry, circuit breaker, timeout policies tanımlı değil. Iyzico API failure durumunda cascade failure olur. Bu skill, resilience policies ekleyerek external service çağrılarını güvenli hale getirir.
---

# Add Polly Resilience Policies

## Problem

**Risk Level:** CRITICAL

`Microsoft.Extensions.Http.Polly` package var ama kullanılmıyor. External service (Iyzico) failure durumunda:
- Network glitch → unnecessary failures
- Iyzico API timeout → kullanıcı ödeme yapamıyor
- Cascade failures across system

**Affected File:**
- `src/Infrastructure/Platform.Integration/Extensions/IntegrationServiceExtensions.cs`

## Solution Steps

### Step 1: Create Polly Policies Class

Create file: `src/Infrastructure/Platform.Integration/Policies/HttpPolicies.cs`

```csharp
using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace Platform.Integration.Policies;

/// <summary>
/// HTTP client resilience policies for external services.
/// </summary>
public static class HttpPolicies
{
    /// <summary>
    /// Retry policy: 3 attempts with exponential backoff.
    /// Retries on transient errors and 5xx responses.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => (int)msg.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),  // 2s, 4s, 8s
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    var logger = context.TryGetValue("logger", out var l) ? l as ILogger : null;
                    logger?.LogWarning(
                        "HTTP retry {RetryCount} after {Delay}s. Status: {Status}",
                        retryCount, timeSpan.TotalSeconds, outcome.Result?.StatusCode);
                });
    }

    /// <summary>
    /// Circuit breaker: Opens after 5 consecutive failures, stays open for 30s.
    /// Prevents cascade failures when external service is down.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    // Log circuit open
                },
                onReset: () =>
                {
                    // Log circuit closed
                });
    }

    /// <summary>
    /// Timeout policy: Fails after 30 seconds.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Combined policy wrap: Retry → Circuit Breaker → Timeout
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy.WrapAsync(
            GetTimeoutPolicy(),
            GetCircuitBreakerPolicy(),
            GetRetryPolicy());
    }
}
```

### Step 2: Update IntegrationServiceExtensions

Modify `src/Infrastructure/Platform.Integration/Extensions/IntegrationServiceExtensions.cs`:

```csharp
using Platform.Integration.Policies;

public static IServiceCollection AddIntegrations(
    this IServiceCollection services,
    IConfiguration config,
    ILogger? logger = null)
{
    // Iyzico configuration
    services.AddOptions<IyzicoOptions>()
        .Bind(config.GetSection(IyzicoOptions.SectionName))
        .ValidateDataAnnotations()
        .Validate(opt =>
        {
            if (opt.IsProduction && opt.BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Sandbox URL cannot be used in production.");
            if (opt.IsProduction && !opt.CallbackUrl.StartsWith("https://"))
                throw new InvalidOperationException("CallbackUrl must use HTTPS in production.");
            return true;
        })
        .ValidateOnStart();

    // Iyzico HTTP client with resilience policies
    services.AddHttpClient<IIyzicoService, IyzicoService>()
        .ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);  // Overall timeout
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            EnableMultipleHttp2Connections = true
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var logger = serviceProvider.GetService<ILogger<IyzicoService>>();
            var context = new Context { ["logger"] = logger };
            return HttpPolicies.GetCombinedPolicy().WithPollyContext(context);
        });

    logger?.LogInformation("Integration services registered with Polly resilience policies.");
    return services;
}
```

### Step 3: Add Polly Package (if not already)

```xml
<!-- In Platform.Integration.csproj -->
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="10.0.3" />
```

## Verification

### Test Circuit Breaker

```bash
# 1. Stop Iyzico mock server
# 2. Make 5+ requests - circuit should open
# 3. Requests should fail immediately (no timeout wait)
# 4. Wait 30s - circuit should close
# 5. Resume server - requests should succeed
```

### Test Retry Policy

```bash
# 1. Configure Iyzico to return 503 temporarily
# 2. Make request - should retry 3 times
# 3. Check logs for retry attempts
```

## Monitoring

Add circuit breaker state to health checks:

```csharp
// In health checks
builder.Services.AddHealthChecks()
    .AddCheck<CircuitBreakerHealthCheck>("circuit-breaker");
```

## Priority

**IMMEDIATE** - Production resilience için kritik.
