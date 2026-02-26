using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Health check endpoints.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder RegisterHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Basic liveness - always returns 200 if app is running
        app.MapGet("/health/live", () => Results.Ok(new
        {
            Status = "Alive",
            Timestamp = DateTime.UtcNow
        }))
        .WithName("Liveness")
        .WithTags("Health")
        .ExcludeFromDescription();

        // Readiness - checks all dependencies tagged "ready"
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        })
        .ExcludeFromDescription();

        // Detailed health - all checks
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
