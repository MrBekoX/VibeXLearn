---
name: fix-refresh-token-performance
description: AuthService.RefreshAsync metodunda O(n) query sorunu var - tüm kullanıcıları memory'de tarıyor. Production'da user sayısı arttıkça sistem çöker. Bu skill, refresh token lookup'ı optimize ederek direct DB query'e dönüştürür.
---

# Fix O(n) Refresh Token Lookup

## Problem

**Risk Level:** CRITICAL

`AuthService.RefreshAsync` tüm kullanıcıları iterate ederek refresh token arıyor. 1000 kullanıcıda 1000 DB query. Production'da bu sistem çöker.

**Affected File:**
- `src/Infrastructure/Platform.Infrastructure/Services/AuthService.cs` (lines 100-112)

**Current Problematic Code:**
```csharp
// SORUNLU KOD:
foreach (var u in users)  // Tüm kullanıcılar memory'de!
{
    var loadedUser = await userManager.FindByIdAsync(u.Id.ToString());
    if (loadedUser is null || loadedUser.IsDeleted) continue;

    existingToken = loadedUser.GetActiveRefreshToken(refreshToken);
    if (existingToken is not null)
    {
        user = loadedUser;
        break;
    }
}
```

## Solution Steps

### Step 1: Add Direct Query Method to AuthService

Modify `src/Infrastructure/Platform.Infrastructure/Services/AuthService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

// Add DbContext dependency
public sealed class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext dbContext,  // ← ADD THIS
    IConfiguration config,
    ILogger<AuthService> logger) : IAuthService
```

### Step 2: Replace O(n) Loop with Direct Query

```csharp
public async Task<Result<(string AccessToken, DateTime ExpiresAt, string NewRefreshToken)>> RefreshAsync(
    string refreshToken, CancellationToken ct)
{
    // DIRECT DB QUERY - O(1) instead of O(n)
    var user = await dbContext.Users
        .Include(u => u.RefreshTokens)
        .Where(u => !u.IsDeleted)
        .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken), ct);

    if (user is null)
        return Result.Fail<(string, DateTime, string)>(
            "AUTH_INVALID_REFRESH_TOKEN", AuthBusinessMessages.InvalidRefreshToken);

    var existingToken = user.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken);
    if (existingToken is null || !existingToken.IsActive)
        return Result.Fail<(string, DateTime, string)>(
            "AUTH_INVALID_REFRESH_TOKEN", AuthBusinessMessages.InvalidRefreshToken);

    // Token reuse attack check
    if (existingToken.WasReplacedAfter(refreshToken))
    {
        logger.LogWarning("{Event} | UserId:{UserId}",
            SecurityAuditEvents.TokenRevoked, user.Id);
        user.RevokeAllRefreshTokens();
        await userManager.UpdateAsync(user);
        return Result.Fail<(string, DateTime, string)>(
            "AUTH_TOKEN_REUSE", "Token reuse detected. All tokens revoked.");
    }

    // Rotate: revoke old, create new
    var newRefreshTokenString = GenerateRefreshTokenString();
    existingToken.RevokeAndReplace(newRefreshTokenString);

    var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenString, RefreshTokenLifetime);
    user.AddRefreshToken(newRefreshToken);

    await dbContext.SaveChangesAsync(ct);  // Direct save

    var roles = await userManager.GetRolesAsync(user);
    var (accessToken, expiresAt) = GenerateAccessToken(user, roles);

    logger.LogInformation("{Event} | UserId:{UserId}",
        SecurityAuditEvents.TokenRefreshed, user.Id);

    return Result.Success((accessToken, expiresAt, newRefreshTokenString));
}
```

### Step 3: Add Index for Performance

Add to `src/Infrastructure/Platform.Persistence/Configurations/RefreshTokenConfiguration.cs`:

```csharp
// Ensure index on Token column for fast lookup
builder.HasIndex(rt => rt.Token).IsUnique();
builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked });
```



## Verification

1. **Benchmark before/after:**
   ```bash
   # With 1000 users in DB
   ab -n 100 -c 10 http://localhost:8080/api/v1/auth/refresh
   ```

2. **Check query count:**
   - Before: ~1000 queries per refresh
   - After: 1-2 queries per refresh

## Expected Impact

| Metric | Before | After |
|--------|--------|-------|
| Query count | O(n) | O(1) |
| Response time | 500ms+ | <50ms |
| Memory usage | High | Low |

## Priority

**IMMEDIATE** - Production'da scalability sorunu yaratır.
