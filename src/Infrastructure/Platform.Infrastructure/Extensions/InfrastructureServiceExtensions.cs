using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Cache;
using Platform.Application.Common.Services;
using Platform.Infrastructure.Locking;
using Platform.Infrastructure.Middlewares;
using Platform.Infrastructure.Serialization;
using Platform.Infrastructure.Services;
using Platform.Infrastructure.Services.Tagging;
using StackExchange.Redis;

namespace Platform.Infrastructure.Extensions;

/// <summary>
/// Infrastructure katmanı DI extension'ları.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── CacheSettings (typed options) ──────────────────────────────────
        services.Configure<CacheSettings>(config.GetSection(CacheSettings.SectionName));

        // ── L1: In-process memory cache ────────────────────────────────────
        services.AddMemoryCache();

        // ── L2: Redis (IConnectionMultiplexer — thread-safe singleton) ─────
        var redisConnectionString = config.GetConnectionString("Redis")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Redis is required. Set it via environment variable.");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        // IDistributedCache (still used by ASP.NET session / other components if needed)
        services.AddStackExchangeRedisCache(opt =>
        {
            opt.Configuration = redisConnectionString;
        });

        // ── Phase 1: Stampede Protection (Lock Provider) ─────────────────
        services.AddSingleton<ICacheLockProvider>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;
            if (settings.EnableDistributedLocking)
                return new DistributedCacheLockProvider(
                    sp.GetRequiredService<IConnectionMultiplexer>(),
                    settings);
            return new LocalCacheLockProvider();
        });

        // ── Phase 2: L1 Synchronization (Pub/Sub Subscriber) ────────────
        services.AddSingleton<L1InvalidationSubscriber>();
        services.AddHostedService(sp => sp.GetRequiredService<L1InvalidationSubscriber>());

        // ── Phase 3: Serialization ───────────────────────────────────────
        services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();

        // ── Phase 4: Tag-Based Invalidation ──────────────────────────────
        services.AddSingleton<ICacheTagManager, CacheTagManager>();

        // ── Cache Metrics (System.Diagnostics.Metrics) ──────────────────────
        services.AddSingleton<CacheMetrics>();

        // ── CacheService: Singleton (IMemoryCache + IConnectionMultiplexer are thread-safe) ──
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<ICacheTtlResolver, CacheTtlResolver>();

        // ── Cache Warmup (BackgroundService) ─────────────────────────────
        services.AddHostedService<CacheWarmupService>();

        // ── Business rules engine ──────────────────────────────────────────
        services.AddSingleton<Application.Common.Rules.IBusinessRuleEngine,
            Application.Common.Rules.BusinessRuleEngine>();

        // ── Auth service ──────────────────────────────────────────────────────
        services.AddScoped<Application.Common.Interfaces.IAuthService,
            Services.AuthService>();

        // ── HTTP client for health checks ─────────────────────────────────
        services.AddHttpClient("elasticsearch-health");

        // ── Token blacklist (Redis-based JWT revocation) ──────────────────
        services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

        // ── Token cleanup ──────────────────────────────────────────────────
        services.AddScoped<ITokenCleanupService, TokenCleanupService>();
        services.AddHostedService<BackgroundServices.RefreshTokenCleanupService>();

        // ── JWT Key Rotation Service (SKILL: jwt-asymmetric-keys) ──────────
        services.AddSingleton<KeyRotationService>();
        services.AddHostedService(sp => sp.GetRequiredService<KeyRotationService>());

        // ── Domain Event Dispatcher (SKILL: fix-domain-architecture) ───────
        services.AddScoped<Application.Common.Interfaces.IDomainEventDispatcher,
            Services.DomainEventDispatcher>();

        return services;
    }

    public static IApplicationBuilder UseSecurityMiddlewares(this IApplicationBuilder app)
    {
        // SKILL: request-size-limits - Early request size validation
        app.UseMiddleware<RequestSizeValidationMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // SKILL: jwt-asymmetric-keys - Key rotation headers
        app.UseMiddleware<KeyRotationMiddleware>();

        return app;
    }
}
