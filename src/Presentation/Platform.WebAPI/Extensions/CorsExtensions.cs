namespace Platform.WebAPI.Extensions;

/// <summary>
/// CORS extension'ları.
/// </summary>
public static class CorsExtensions
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(opt =>
        {
            opt.AddPolicy("Frontend", policy => policy
                .WithOrigins(allowedOrigins)
                .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                .WithHeaders("Authorization", "Content-Type", "X-Correlation-ID")
                .AllowCredentials());
        });

        return services;
    }
}
