using Platform.Infrastructure.HealthChecks;

namespace Platform.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure health checks.
/// </summary>
public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration config)
    {
        builder.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
        builder.AddCheck<TokenCleanupHealthCheck>("token-cleanup", tags: ["ready"]);

        var elasticUrl = config.GetConnectionString("Elasticsearch");
        if (!string.IsNullOrEmpty(elasticUrl))
        {
            builder.AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: ["ready"]);
        }

        return builder;
    }
}
