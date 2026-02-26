---
name: add-jwt-token-blacklist
description: JWT token'lar revoke edilemiyor. jti claim var ama Redis blacklist yok. Kullanıcı logout yapsa bile access token 15 dakika aktif kalır. Compromised token kullanılmaya devam edebilir. Bu skill, Redis-based token blacklist mekanizması ekler.
---

# Add JWT Token Blacklist/Revocation

## Problem

**Risk Level:** HIGH

JWT token'lar revoke edilemiyor. `jti` claim var ama Redis blacklist mekanizması yok. Kullanıcı logout yapsa bile mevcut access token 15 dakika kullanılmaya devam edebilir.

**Affected Files:**
- `src/Infrastructure/Platform.Infrastructure/Services/AuthService.cs`
- `src/Presentation/Platform.WebAPI/Extensions/JwtExtensions.cs`

## Solution Steps

### Step 1: Create Token Blacklist Interface

Create file: `src/Core/Platform.Application/Common/Interfaces/ITokenBlacklistService.cs`

```csharp
namespace Platform.Application.Common.Interfaces;

/// <summary>
/// JWT token blacklist service for immediate revocation.
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token's JTI to the blacklist.
    /// </summary>
    Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Checks if a token's JTI is blacklisted.
    /// </summary>
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default);
}
```

### Step 2: Create Redis Implementation

Create file: `src/Infrastructure/Platform.Infrastructure/Services/TokenBlacklistService.cs`

```csharp
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Redis-based JWT token blacklist.
/// Uses SET NX EX for atomic operations.
/// </summary>
public sealed class TokenBlacklistService(
    IConnectionMultiplexer redis,
    ILogger<TokenBlacklistService> logger) : ITokenBlacklistService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string KeyPrefix = "blacklist:jti:";

    public async Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = $"{KeyPrefix}{jti}";
        await _db.StringSetAsync(key, "1", ttl);
        logger.LogDebug("Token blacklisted: {Jti}", jti[..8] + "***");
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default)
    {
        var key = $"{KeyPrefix}{jti}";
        return await _db.KeyExistsAsync(key);
    }
}
```

### Step 3: Register in DI

Add to `src/Infrastructure/Platform.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`:

```csharp
services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
```

### Step 4: Update Logout to Blacklist Token

Modify `src/Infrastructure/Platform.Infrastructure/Services/AuthService.cs`:

```csharp
public sealed class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext dbContext,
    IConfiguration config,
    ITokenBlacklistService tokenBlacklist,  // ← ADD
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result> LogoutAsync(Guid userId, string? currentJti, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail("AUTH_USER_NOT_FOUND", AuthBusinessMessages.UserNotFound);

        // Revoke all refresh tokens
        user.RevokeAllRefreshTokens();
        await userManager.UpdateAsync(user);

        // Blacklist current access token (if JTI provided)
        if (!string.IsNullOrEmpty(currentJti))
        {
            await tokenBlacklist.BlacklistAsync(
                currentJti,
                TimeSpan.FromMinutes(15),  // AccessTokenLifetime
                ct);

            logger.LogInformation("{Event} | UserId:{UserId} | Jti:{Jti}",
                SecurityAuditEvents.TokenRevoked, userId, currentJti[..8] + "***");
        }

        return Result.Success();
    }
}
```

### Step 5: Add JWT Validation Event

Modify `src/Presentation/Platform.WebAPI/Extensions/JwtExtensions.cs`:

```csharp
public static IServiceCollection AddJwtAuthentication(
    this IServiceCollection services,
    IConfiguration config)
{
    // ... existing code ...

    opt.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti)) return;

            var blacklist = context.HttpContext.RequestServices
                .GetRequiredService<ITokenBlacklistService>();

            if (await blacklist.IsBlacklistedAsync(jti, context.HttpContext.RequestAborted))
            {
                context.Fail("Token has been revoked.");
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<JwtBearerEvents>>();
                logger.LogWarning("Revoked token attempted: {Jti}", jti[..8] + "***");
            }
        }
    };

    return services;
}
```

### Step 6: Update Logout Endpoint

Modify `src/Presentation/Platform.WebAPI/Endpoints/AuthEndpoints.cs`:

```csharp
group.MapPost("/logout", async (
    IAuthService authService,
    ICurrentUserService currentUser,
    HttpContext http,
    CancellationToken ct) =>
{
    var jti = http.User?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    var result = await authService.LogoutAsync(currentUser.UserId!.Value, jti, ct);

    // Clear refresh token cookie
    http.Response.Cookies.Delete("refresh_token", new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth"
    });

    return result.IsSuccess
        ? Results.Ok(new { message = "Logged out successfully" })
        : Results.BadRequest(new { error = result.Error.Message });
})
.RequireAuthorization();
```

## Verification

```bash
# 1. Login and get token
TOKEN=$(curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test123!"}' | jq -r '.accessToken')

# 2. Access protected endpoint
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/v1/auth/profile
# Should succeed

# 3. Logout
curl -X POST -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/v1/auth/logout

# 4. Try same token again
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/v1/auth/profile
# Should return 401 Unauthorized
```

## Priority

**HIGH** - Token compromise durumunda protection sağlar.
