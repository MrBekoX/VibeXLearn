using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Interfaces;
using Platform.Integration.Iyzico;
using Platform.Integration.Policies;

namespace Platform.Integration.Extensions;

/// <summary>
/// Integration katmanı DI extension'ları.
/// </summary>
public static class IntegrationServiceExtensions
{
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
                if (opt.IsProduction &&
                    opt.BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "CRITICAL: Sandbox Iyzico URL cannot be used in Production.");

                if (opt.IsProduction && !opt.CallbackUrl.StartsWith("https://"))
                    throw new InvalidOperationException(
                        "CRITICAL: Iyzico CallbackUrl must use HTTPS in Production.");

                if (opt.IsProduction &&
                    (opt.DefaultBuyerPhone == "+905555555555" ||
                     opt.DefaultBuyerIdentityNumber == "11111111111"))
                    throw new InvalidOperationException(
                        "CRITICAL: Iyzico buyer defaults must be overridden in Production.");

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
            .AddPolicyHandler(_ => HttpPolicies.GetCombinedPolicy());

        // GitHub client (opsiyonel - PAT varsa register et)
        var gitHubPat = config["ExternalServices:GitHub:PAT"];
        if (!string.IsNullOrWhiteSpace(gitHubPat))
        {
            // services.AddHttpClient<IGitHubClient, GitHubClient>();
            logger?.LogInformation("GitHub integration enabled.");
        }
        else
        {
            logger?.LogWarning("GitHub PAT not configured. GitHub features disabled.");
        }

        logger?.LogInformation("Integration services registered with Polly resilience policies.");
        return services;
    }
}
