using Asp.Versioning;

namespace Platform.WebAPI.Extensions;

/// <summary>
/// API Versioning with deprecation/sunset support.
/// </summary>
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningSetup(this IServiceCollection services)
    {
        var apiVersioning = services.AddApiVersioning(opt =>
        {
            // Default version
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ReportApiVersions = true; // Returns api-supported-versions header

            // Multi-scheme version reader
            opt.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version")
            );

            // Sunset policy for v1.0 — effective 6 months from deployment
            opt.Policies.Sunset(new ApiVersion(1, 0))
                .Effective(DateTimeOffset.UtcNow.AddMonths(6))
                .Link("https://docs.vibexlearn.com/api/migration-v1-to-v2")
                    .Title("Migration Guide V1 to V2")
                    .Type("text/html");
        });

        apiVersioning.AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
