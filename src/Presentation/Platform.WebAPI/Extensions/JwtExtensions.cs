using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Platform.Infrastructure.Services;
using Platform.WebAPI.Options;

namespace Platform.WebAPI.Extensions;

/// <summary>
/// JWT authentication extension'ları.
/// SKILL: jwt-asymmetric-keys - Supports both HS256 (symmetric) and RS256 (asymmetric) keys
/// </summary>
public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Bind and validate JWT options
        var jwtOptions = new JwtOptions();
        config.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

        ValidateJwtOptions(jwtOptions, config);

        var issuer = jwtOptions.Issuer;
        var audience = jwtOptions.Audience;

        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        // Configure JWT bearer options via DI - avoids BuildServiceProvider anti-pattern
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<KeyRotationService>((opt, keyRotationService) =>
            {
                opt.TokenValidationParameters = BuildTokenValidationParameters(
                    issuer,
                    audience,
                    keyRotationService);

                opt.Events = BuildJwtEvents();
            });

        services.AddAuthorization();

        return services;
    }

    private static TokenValidationParameters BuildTokenValidationParameters(
        string issuer,
        string audience,
        KeyRotationService keyRotationService)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ClockSkew                = TimeSpan.Zero,
            ValidIssuer              = issuer,
            ValidAudience            = audience,
            IssuerSigningKeyResolver = (_, _, kid, _) =>
            {
                var key = keyRotationService.GetValidationKey(kid);
                return key is not null
                    ? [key]
                    : [keyRotationService.CurrentValidationKey];
            }
        };
    }

    private static JwtBearerEvents BuildJwtEvents()
    {
        return new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(jti)) return;

                var blacklist = context.HttpContext.RequestServices
                    .GetRequiredService<Platform.Application.Common.Interfaces.ITokenBlacklistService>();

                if (await blacklist.IsBlacklistedAsync(jti, context.HttpContext.RequestAborted))
                {
                    context.Fail("Token has been revoked.");
                    var tokenLogger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    tokenLogger.LogWarning(
                        "Revoked token attempted: {Jti}",
                        jti[..Math.Min(8, jti.Length)] + "***");
                }
            },
            OnAuthenticationFailed = ctx =>
            {
                var authLogger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<JwtBearerEvents>>();
                authLogger.LogWarning("JWT authentication failed: {Error}", ctx.Exception?.Message);
                return Task.CompletedTask;
            }
        };
    }

    private static void ValidateJwtOptions(JwtOptions options, IConfiguration config)
    {
        if (string.IsNullOrEmpty(options.Issuer))
            throw new InvalidOperationException("Jwt:Issuer is required.");

        if (string.IsNullOrEmpty(options.Audience))
            throw new InvalidOperationException("Jwt:Audience is required.");

        // Check if RSA keys are configured
        var hasRsaKeys = !string.IsNullOrEmpty(options.RsaPrivateKeyPem) ||
                         !string.IsNullOrEmpty(options.RsaPrivateKeyPath);

        // If no RSA keys, symmetric secret is required
        if (!hasRsaKeys)
        {
            var secret = config["Jwt:Secret"]
                ?? throw new InvalidOperationException(
                    "Jwt:Secret is required when RSA keys are not configured. " +
                    "Set it via environment variable or user-secrets.");

            if (secret.Length < 32)
                throw new InvalidOperationException(
                    $"Jwt:Secret must be at least 32 characters for HS256 security. Current length: {secret.Length}");
        }
    }
}
