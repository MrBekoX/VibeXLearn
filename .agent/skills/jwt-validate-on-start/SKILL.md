---
name: jwt-validate-on-start
description: JWT secret'ın varlığı kontrol ediliyor ama ValidateOnStart kullanılmıyor. Eksik Issuer, Audience veya kısa secret ile uygulama başlar, ilk auth request'te hata verir. Bu skill, JWT options validation'ı startup'a taşır.
---

# Add JWT ValidateOnStart

## Problem

**Risk Level:** MEDIUM

JWT secret'ın varlığı kontrol ediliyor ama `ValidateOnStart()` kullanılmıyor. Eksik `Issuer`, `Audience` veya kısa secret ile uygulama başlar, ilk auth request'te hata verir.

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Extensions/JwtExtensions.cs`

## Solution Steps

### Step 1: Create JwtOptions Class

Create: `src/Core/Platform.Application/Common/Options/JwtOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Platform.Application.Common.Options;

/// <summary>
/// JWT configuration options with validation.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "JWT Secret is required")]
    [MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters for HS256 security")]
    public string Secret { get; init; } = default!;

    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; init; } = default!;

    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; init; } = default!;

    [Range(1, 1440, ErrorMessage = "Access token expiration must be between 1 and 1440 minutes")]
    public int AccessTokenExpirationMinutes { get; init; } = 15;

    [Range(1, 365, ErrorMessage = "Refresh token expiration must be between 1 and 365 days")]
    public int RefreshTokenExpirationDays { get; init; } = 7;

    /// <summary>
    /// Validates that the configuration is secure for production.
    /// </summary>
    public bool IsSecureSecret()
    {
        // Check for common insecure patterns
        if (string.IsNullOrEmpty(Secret)) return false;
        if (Secret.Length < 32) return false;
        if (Secret.Equals("secret", StringComparison.OrdinalIgnoreCase)) return false;
        if (Secret.Equals("changeme", StringComparison.OrdinalIgnoreCase)) return false;
        if (Secret.Contains("test", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }
}
```

### Step 2: Update JwtExtensions with ValidateOnStart

Update `src/Presentation/Platform.WebAPI/Extensions/JwtExtensions.cs`:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Platform.Application.Common.Options;

namespace Platform.WebAPI.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Register and validate JWT options ON STARTUP
        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .Validate(options =>
            {
                // Secret validation
                if (string.IsNullOrWhiteSpace(options.Secret))
                    throw new InvalidOperationException(
                        "JWT:Secret is required. Set it via environment variable or user-secrets.");

                if (options.Secret.Length < 32)
                    throw new InvalidOperationException(
                        $"JWT:Secret must be at least 32 characters for HS256 security. Current length: {options.Secret.Length}");

                // Issuer validation
                if (string.IsNullOrWhiteSpace(options.Issuer))
                    throw new InvalidOperationException(
                        "JWT:Issuer is required.");

                // Audience validation
                if (string.IsNullOrWhiteSpace(options.Audience))
                    throw new InvalidOperationException(
                        "JWT:Audience is required.");

                // Production security check
                var env = config["ASPNETCORE_ENVIRONMENT"];
                if (env == "Production" && !options.IsSecureSecret())
                {
                    throw new InvalidOperationException(
                        "CRITICAL: JWT Secret appears to be insecure for production. " +
                        "Use a strong, randomly generated secret of at least 32 characters.");
                }

                return true;
            })
            .ValidateOnStart(); // ← This ensures validation happens at startup

        // Get options for JWT configuration
        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,

                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Secret))
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();

                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        logger.LogWarning("Token expired: {Message}", context.Exception.Message);
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    else if (context.Exception is SecurityTokenInvalidSignatureException)
                    {
                        logger.LogWarning("Invalid token signature");
                    }
                    else
                    {
                        logger.LogError(context.Exception, "Authentication failed");
                    }

                    return Task.CompletedTask;
                },

                OnTokenValidated = async context =>
                {
                    // Add token blacklist check here if implemented
                    await Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
```

### Step 3: Update Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add JWT authentication with startup validation
builder.Services.AddJwtAuthentication(builder.Configuration);
```

### Step 4: Add Environment Variable Documentation

Update `.env.example`:

```bash
# ── JWT Configuration ───────────────────────────────────────────────────
# IMPORTANT: In production, use environment variables or a secrets manager
# JWT Secret must be at least 32 characters for HS256 security

JWT_SECRET=your_minimum_32_character_secret_key_here
JWT_ISSUER=VibeXLearnPlatform
JWT_AUDIENCE=VibeXLearnPlatform.Clients
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRATION_DAYS=7
```

### Step 5: Add Development User Secrets

```bash
# For development, use user-secrets instead of appsettings.json
cd src/Presentation/Platform.WebAPI

dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "your_development_secret_key_minimum_32_chars"
dotnet user-secrets set "Jwt:Issuer" "VibeXLearnPlatform"
dotnet user-secrets set "Jwt:Audience" "VibeXLearnPlatform.Clients"
```

### Step 6: Remove Secrets from appsettings.json

Ensure `appsettings.json` and `appsettings.Development.json` do NOT contain the actual secret:

```json
// appsettings.json - NO SECRET
{
  "Jwt": {
    "Issuer": "VibeXLearnPlatform",
    "Audience": "VibeXLearnPlatform.Clients",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
  // Secret should come from environment variable or user-secrets
}
```

## Verification

```bash
# Test 1: Start without JWT secret - should FAIL at startup
unset JWT_SECRET
dotnet run --project src/Presentation/Platform.WebAPI
# Expected: InvalidOperationException: JWT:Secret is required

# Test 2: Start with short secret - should FAIL at startup
export JWT_SECRET="short"
dotnet run --project src/Presentation/Platform.WebAPI
# Expected: InvalidOperationException: JWT:Secret must be at least 32 characters

# Test 3: Start with valid configuration - should SUCCEED
export JWT_SECRET="your_minimum_32_character_secret_key_here"
dotnet run --project src/Presentation/Platform.WebAPI
# Expected: Application starts successfully
```

## Priority

**IMMEDIATE** - Prevents runtime authentication failures.
