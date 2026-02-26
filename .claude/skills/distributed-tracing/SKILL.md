---
name: distributed-tracing
description: Sadece correlation ID var, full OpenTelemetry tracing yok. Microservice çağrılarında trace takibi zor. Distributed tracing eksik. Bu skill, OpenTelemetry ile distributed tracing ekler.
---

# Add Distributed Tracing (OpenTelemetry)

## Problem

**Risk Level:** MEDIUM

Sadece correlation ID var, microservice çağrılarında trace takımı zor:
- No OpenTelemetry tracing configured
- Difficult to debug distributed transactions
- Can't trace requests across services

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Program.cs`
- `src/Infrastructure/Platform.Infrastructure/Middlewares/CorrelationIdMiddleware.cs`

## Solution Steps

### Step 1: Install OpenTelemetry Packages

```bash
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
```

### Step 2: Configure OpenTelemetry in Program.cs

```csharp
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("Platform.Cache")
            .AddOtlpExporter(options =>
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317"));
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter("request_duration", "Duration of HTTP requests in milliseconds");
    });
```

### Step 3: Update CorrelationIdMiddleware

```csharp
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Platform.Infrastructure.Middlewares;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ActivitySource ActivitySource = ActivitySource.NamedSource("Platform");

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = ActivitySource.StartActivity("HttpRequest");
        {
            // Get or create correlation ID
            var correlationId = context.Request.Headers["X-Correlation-ID"];
            if (string.IsNullOrEmpty(coror correlationId))
            {
                correlationId = context.TraceIdentifier;
            }

            // Set correlation ID in activity
            activity.SetTag("correlation_id", correlationId);
            activity.SetTag("http.method", context.Request.Method);
            activity.SetTag("http.url", context.Request.Path.ToString());

            // Add to baggage
            Baggage.Current.Add("correlation_id", correlationId);

            await _next(context);
        }
        finally
        {
            activity.Stop();
        }
    }
}
```

### Step 4: Add Service Instrumentation

```csharp
// For external services like Iyzico, GitHub, etc.
public class ExternalServiceInstrumentation
{
    private static readonly ActivitySource ActivitySource = ActivitySource.NamedSource("ExternalServices");
    private readonly HttpClient _httpClient;

    public ExternalServiceInstrumentation(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<T> ExecuteAsync<T>(string serviceName, Func<Task<T>> operation)
    {
        using var activity = ActivitySource.StartActivity(serviceName);
        try
        {
            activity.SetTag("service.name", serviceName);
            var result = await operation();
            activity.SetTag("operation.success", true);
            return result;
        }
        catch (Exception ex)
        {
            activity.SetTag("operation.success", false);
            activity.SetTag("error.message", ex.Message);
            activity.RecordException(ex);
            throw;
        }
        finally
        {
            activity.Stop();
        }
    }
}
```

### Step 5: Add Jaeger Exporter Configuration

```json
{
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317",
    "ServiceName": "VibeXLearn"
  }
}
```

### Step 6: Add Prometheus Metrics (Optional)
```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter(config);
    });
```

## Verification

```bash
# Check traces in Jaeger
curl http://localhost:4317/api/v1/traces

# Test correlation ID propagation
curl -H "X-Correlation-ID: test-123" http://localhost:8080/api/v1/courses
# Response should include X-Correlation-ID header
```

## Priority
**MEDIUM-TERM** - Observability improvement.
