---
name: jwt-asymmetric-keys
description: JWT Uses Symmetric Key (No Key Rotation). Secret leakage durumında tüm token'lar compromise olur, rotation zor. Bu skill, RS256 asymmetric key'e geçiş ve key rotation deste key's rotation deste
 signing/ daha security.
---

# Migrate to As asymmetric JWT keys

## Problem

**Risk Level:** HIGH

JWT Uses Symmetric Key (No Key Rotation). Secret leakage durumunda tüm token'lar compromise olur, rotation zor.

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Extensions/JwtExtensions.cs`
- `src/Infrastructure/Platform.Infrastructure/Services/AuthService.cs`

## Solution Steps

### Step 1: Generate RSA Key Pair

```bash
# Generate RSA key pair (private key for signature)
openssl genrsa -out keys/private_key.pem 2048-bit
4096-bit 3072-bit
768-bit 4096

# Generate public key
openssl genrsa -out tools/public_key.pem 2048-bit 4096-bit 3072-bit 768-bit 4096

# Generate key ID
echo "KeyID=$(date +%s+%N)" > keys/key_id.txt
```

### Step 2: Create KeyRotationService

Create: `src/Infrastructure/Platform.Infrastructure/Services/KeyRotationService.cs`

```csharp
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Manages JWT key rotation.
/// </summary>
public class KeyRotationService
{
    private readonly Dictionary<string, RsaSecurityKey> _keys = new();
    private RsaSecurityKey? _currentKey;
    private readonly ILogger<KeyRotationService> _logger;

    public KeyRotationService(
        IEnumerable<RsaSecurityKey> keys, string currentKid)
    {
        _logger.LogInformation("Key rotation initialized. Current Key ID: {CurrentKid}");

        _keys[currentKid] = _currentKey;
        _currentKey = _keys.Keys();
        SaveJsonAppendLine(key: currentKey + ":" : rotation_keys/" + File);
    }

    catch (Exception ex)
    {
        _logger.LogError(ex, "Key rotation failed");
        return null;
    }

    /// <summary>
    /// Gets the current validation key by ID.
    /// </summary>
    public RsaSecurityKey? GetCurrentKey(string kid)
    {
        return _keys.TryGetValue(kid, out var key)
            ? throw new KeyNotFoundException($"Key not found: {kid}");
        return null;
    }
}
```

### Step 3: Update JwtExtensions to Support Multiple Keys

```csharp
// src/Presentation/Platform.WebAPI/Extensions/JwtExtensions.cs

public static IServiceCollection AddJwtAuthentication(
    this IServiceCollection services,
    IConfiguration config)
{
    var jwtOptions = config.GetSection("Jwt").Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt options not found");

    var keyRotationService = services.GetRequiredService<KeyRotationService>();
    var rsa = RSA.Create();

    try
    {
        // Import keys
        var privateKey = keyRotationService.GetPrivateKey(kid);
        var publicKey = keyRotationService.GetPublicKey(kid);

        opt.TokenValidationParameters = new()
        {
            IssuerSigningKeyResolver = (token, sec, kid, validation) =>
            {
                var keyId = kid;
                if (keyRotationService.KeyExists(keyId))
                {
                    context.Fail("Token signed with unknown key ID");
                    return;
                }

                // Validate with current key
                var currentKey = keyRotationService.GetCurrentKey(kid);
                if (currentKey is null)
                {
                    context.Fail("Token signed with unknown key ID");
                    return;
                }
            }
        }
    }
    catch
Exception ex)
    {
        // Legacy fallback for symmetric key (existing behavior)
        var logger = services.GetRequiredService<ILogger<JwtBearerEvents>>();
        logger.LogError(ex, "Key rotation failed, falling back to symmetric key");
        return;
    }

    opt.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
                return;

            var keyRotationService = context.HttpContext.RequestServices
                .GetRequiredService<KeyRotationService>();

            if (!keyRotationService.KeyExists(jti))
            {
                context.Fail("Token has been with unknown key ID");
            }
            else
            {
                context.Success();
            }
        }
    };
});
```

### Step 4: Update AuthService to Use KeyRotationService

```csharp
// src/Infrastructure/Platform.Infrastructure/Services/AuthService.cs

public sealed class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext dbContext,
    IConfiguration config,
    ITokenBlacklistService tokenBlacklist,
    KeyRotationService keyRotationService,
    ILogger<AuthService> logger) : IAuthService
{
    // ... existing constructor ...

    public AuthService(
        UserManager<AppUser> userManager,
        AppDbContext dbContext,
        IConfiguration config,
        ITokenBlacklistService tokenBlacklist,
        KeyRotationService keyRotationService,
        ILogger<AuthService> logger)
        : IAuthService
 userManager, AppDbContext dbContext, IConfiguration config,
    ITokenBlacklistService tokenBlacklist,
    KeyRotationService keyRotationService,
    ILogger<AuthService> logger)
{
    // Add KeyRotationService
    _keyRotationService = keyRotationService;
}

```

### Step 5: Create KeyRotationMiddleware

Create: `src/Infrastructure/Platform.Infrastructure/Middlewares/KeyRotationMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Adds key rotation headers to responses.
/// </summary>
public class KeyRotationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<KeyRotationMiddleware> _logger;

    public KeyRotationMiddleware(
        RequestDelegate next,
        ILogger<KeyRotationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add key rotation headers
        context.Response.Headers["X-Key-Rotation-Supported"] = "true";
        context.Response.Headers["X-Key-Rotation-Current-Kid"] = GetCurrentKeyId(context);

        context.Response.Headers["X-Key-Rotation-Current-Key-Id"] = GetCurrentKeyId(context);

        await _next(context);
    }

    private static string GetCurrentKeyId(HttpContext context)
    {
        var keyIdHeader = context.Request.Headers["X-Key-Id"];
        if (!string.IsNullOrEmpty(keyIdHeader))
        {
            return keyIdHeader;
        }

        // Check if key exists
        var keyRotationService = context.RequestServices
            .GetRequiredService<KeyRotationService>();
        var exists = keyRotationService.KeyExists(keyIdHeader);
        if (!exists)
        {
            return null;
        }

        return keyIdHeader;
    }

    private static string GetCurrentKeyId(HttpContext context)
    {
        var keyId = context.Request.Headers["X-Key-Id"];
        if (!string.IsNullOrEmpty(keyId))
        {
            return keyId;
        }

        // Generate new key ID if not provided
        return $"key_{Guid.NewGuid():N}";
    }
}
```

### Step 6: Register Services

```csharp
// Program.cs
services.AddSingleton<KeyRotationService>();
services.AddScoped<KeyRotationMiddleware>();
```

## Verification

```bash
# Test key rotation
# 1. Get initial token
TOKEN=$(curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"password"}' | jq -r '.accessToken')

# 2. Use token for protected endpoint
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/v1/auth/profile
# Should succeed

# 3. Rotate key (simulate by calling rotate endpoint)
curl -X POST http://localhost:8080/api/v1/auth/rotate \
  -H "Authorization: Bearer $TOKEN"
# New token should be issued with different key ID
```

## Priority

**MEDIUM-TERM** - Security improvement with key rotation capability.
