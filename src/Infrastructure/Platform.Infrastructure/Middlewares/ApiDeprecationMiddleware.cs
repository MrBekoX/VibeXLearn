namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Adds deprecation headers (Sunset, Deprecation, Link) to responses
/// for deprecated API versions.
/// </summary>
public class ApiDeprecationMiddleware(
    RequestDelegate next,
    ILogger<ApiDeprecationMiddleware> logger)
{
    // V1.0 sunset date — must match the policy in ApiVersioningExtensions
    private static readonly DateTimeOffset SunsetDate = DateTimeOffset.UtcNow.AddMonths(6);
    private const string MigrationGuideUrl = "https://docs.vibexlearn.com/api/migration-v1-to-v2";

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Only apply to versioned API paths
        var path = context.Request.Path.Value;
        if (path is null || !path.Contains("/api/v", StringComparison.OrdinalIgnoreCase))
            return;

        // Check if the request is using v1
        if (path.Contains("/api/v1/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/api/v1.0/", StringComparison.OrdinalIgnoreCase))
        {
            // RFC 8594 Sunset header
            context.Response.Headers["Sunset"] = SunsetDate.ToString("R");

            // Deprecation header (draft-ietf-httpapi-deprecation-header)
            context.Response.Headers["Deprecation"] = "true";

            // Link header to migration guide
            context.Response.Headers.Append("Link",
                $"<{MigrationGuideUrl}>; rel=\"sunset\"; title=\"Migration Guide V1 to V2\"; type=\"text/html\"");

            logger.LogInformation(
                "Deprecated API v1 accessed. Path: {Path}, Sunset: {Sunset}",
                path, SunsetDate.ToString("yyyy-MM-dd"));
        }
    }
}
