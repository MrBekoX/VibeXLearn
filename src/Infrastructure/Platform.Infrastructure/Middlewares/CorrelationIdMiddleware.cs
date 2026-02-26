using System.Diagnostics;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Correlation ID middleware — manages X-Correlation-ID header
/// and links it to the current Activity for distributed tracing.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers.TryGetValue(CorrelationIdHeader, out var id)
            ? id.ToString()
            : ctx.TraceIdentifier;

        ctx.Response.Headers[CorrelationIdHeader] = correlationId;

        // Make available to downstream services
        ctx.Items[CorrelationIdHeader] = correlationId;

        // Link to OpenTelemetry Activity for distributed tracing
        var activity = Activity.Current;
        if (activity is not null)
        {
            activity.SetTag("correlation.id", correlationId);
            activity.SetBaggage("correlation.id", correlationId);
        }

        await next(ctx);
    }
}
