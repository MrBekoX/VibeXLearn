using Microsoft.Extensions.Options;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Options for API deprecation sunset policy.
/// </summary>
public sealed class ApiDeprecationOptions
{
    public const string SectionName = "ApiDeprecation";

    /// <summary>
    /// The date at which the deprecated API version will be removed.
    /// Defaults to 6 months from application start if not configured.
    /// </summary>
    public DateTimeOffset SunsetDate { get; set; } = DateTimeOffset.UtcNow.AddMonths(6);

    /// <summary>
    /// URL to the migration guide documentation.
    /// </summary>
    public string MigrationGuideUrl { get; set; } = "https://docs.vibexlearn.com/api/migration-v1-to-v2";
}

/// <summary>
/// Adds deprecation headers (Sunset, Deprecation, Link) to responses
/// for deprecated API versions.
/// </summary>
public class ApiDeprecationMiddleware(
    RequestDelegate next,
    IOptions<ApiDeprecationOptions> options,
    ILogger<ApiDeprecationMiddleware> logger)
{
    private readonly ApiDeprecationOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to versioned API paths
        var path = context.Request.Path.Value;
        if (path is null || !path.Contains("/api/v", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Check if the request is using v1
        var isDeprecatedV1 =
            path.Contains("/api/v1/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/api/v1.0/", StringComparison.OrdinalIgnoreCase);

        if (isDeprecatedV1)
        {
            // Response başladıktan sonra header yazım hatasını önlemek için OnStarting kullan.
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Sunset"] = _options.SunsetDate.ToString("R");
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers.Append("Link",
                    $"<{_options.MigrationGuideUrl}>; rel=\"sunset\"; title=\"Migration Guide V1 to V2\"; type=\"text/html\"");
                return Task.CompletedTask;
            });
        }

        await next(context);

        if (isDeprecatedV1)
        {
            logger.LogInformation(
                "Deprecated API v1 accessed. Path: {Path}, Sunset: {Sunset}",
                path, _options.SunsetDate.ToString("yyyy-MM-dd"));
        }
    }
}
