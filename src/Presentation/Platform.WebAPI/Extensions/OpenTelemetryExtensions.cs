using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Platform.WebAPI.Extensions;

/// <summary>
/// Configures OpenTelemetry distributed tracing and metrics.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Custom ActivitySource for application-level spans.
    /// </summary>
    public static readonly ActivitySource PlatformActivitySource = new("Platform.API");

    public static IServiceCollection AddOpenTelemetryTracing(
        this IServiceCollection services,
        IConfiguration config)
    {
        if (!config.GetValue("OpenTelemetry:Enabled", false))
            return services;

        var serviceName = config["OpenTelemetry:ServiceName"] ?? "VibeXLearn";
        var otlpEndpoint = config["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: typeof(OpenTelemetryExtensions).Assembly
                        .GetName().Version?.ToString() ?? "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    // Auto-instrumentation
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        // Filter out health check and swagger noise
                        opts.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/health") &&
                            !ctx.Request.Path.StartsWithSegments("/swagger");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    // Custom sources
                    .AddSource("Platform.API")
                    .AddSource("Platform.Cache")
                    // OTLP exporter (Jaeger, Tempo, etc.)
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
            });

        return services;
    }
}
