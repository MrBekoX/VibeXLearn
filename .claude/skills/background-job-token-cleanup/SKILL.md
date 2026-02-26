---
name: background-job-token-cleanup
description: Expired refresh token'lar DB'de birikiyor. Revoked/expired token'lar için cleanup job yok. Bu skill, background service ile token cleanup ekler.
---

# Add Background Job for Refresh Token Cleanup

## Problem

**Risk Level:** MEDIUM

Expired/revoked refresh token'lar veritabanında birikiyor. Bu:
- DB boyutunu gereksiz artırır
- Query performansını düşürür
- Auth tablolarını şişirir

**Affected Files:**
- `src/Core/Platform.Domain/Entities/RefreshToken.cs`
- `src/Infrastructure/Platform.Persistence/Context/AppDbContext.cs`

## Solution Steps

### Step 1: Create Refresh Token Cleanup Service

Create: `src/Infrastructure/Platform.Infrastructure/BackgroundServices/RefreshTokenCleanupService.cs`

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Platform.Persistence.Context;

namespace Platform.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired and revoked refresh tokens.
/// Runs every 24 hours by default.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _retentionPeriod;

    public RefreshTokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<RefreshTokenCleanupService> logger,
        IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Default: run every 24 hours
        _cleanupInterval = config.GetValue("TokenCleanup:IntervalHours", 24) is int hours
            ? TimeSpan.FromHours(hours)
            : TimeSpan.FromHours(24);

        // Default: keep tokens for 30 days after expiration
        _retentionPeriod = config.GetValue("TokenCleanup:RetentionDays", 30) is int days
            ? TimeSpan.FromDays(days)
            : TimeSpan.FromDays(30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Refresh Token Cleanup Service started. Interval: {Interval}, Retention: {Retention}",
            _cleanupInterval, _retentionPeriod);

        // Initial delay before first cleanup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow - _retentionPeriod;

        _logger.LogInformation(
            "Starting token cleanup. Cutoff date: {Cutoff}",
            cutoffDate);

        // Delete expired and revoked tokens older than retention period
        var deletedCount = await dbContext.Set<RefreshToken>()
            .Where(t => (t.IsRevoked || t.ExpiresAt < cutoffDate) && t.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Token cleanup completed. Deleted {Count} expired/revoked tokens",
                deletedCount);
        }
        else
        {
            _logger.LogDebug("Token cleanup completed. No tokens to delete");
        }
    }
}
```

### Step 2: Register Background Service

Add to `src/Infrastructure/Platform.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`:

```csharp
// Background services
services.AddHostedService<RefreshTokenCleanupService>();
```

### Step 3: Add Configuration Options

Add to `appsettings.json`:

```json
{
  "TokenCleanup": {
    "IntervalHours": 24,
    "RetentionDays": 30
  }
}
```

### Step 4: Create Interface for Testing

Create: `src/Core/Platform.Application/Common/Interfaces/ITokenCleanupService.cs`

```csharp
namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Service for cleaning up expired tokens.
/// </summary>
public interface ITokenCleanupService
{
    /// <summary>
    /// Removes expired and revoked tokens older than retention period.
    /// </summary>
    Task<int> CleanupAsync(CancellationToken ct = default);
}
```

Create implementation: `src/Infrastructure/Platform.Infrastructure/Services/TokenCleanupService.cs`

```csharp
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Interfaces;
using Platform.Persistence.Context;

namespace Platform.Infrastructure.Services;

public class TokenCleanupService(
    AppDbContext dbContext,
    ILogger<TokenCleanupService> logger,
    IConfiguration config) : ITokenCleanupService
{
    private readonly int _retentionDays = config.GetValue("TokenCleanup:RetentionDays", 30);

    public async Task<int> CleanupAsync(CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        var deletedCount = await dbContext.Set<RefreshToken>()
            .Where(t => (t.IsRevoked || t.ExpiresAt < DateTime.UtcNow) && t.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("Cleaned up {Count} expired tokens", deletedCount);

        return deletedCount;
    }
}
```

### Step 5: Add Manual Cleanup Endpoint (Admin Only)

Add to `AuthEndpoints.cs`:

```csharp
// Admin-only endpoint for manual cleanup
group.MapPost("/cleanup-tokens", async (
    ITokenCleanupService cleanupService,
    CancellationToken ct) =>
{
    var count = await cleanupService.CleanupAsync(ct);
    return Results.Ok(new { deletedCount = count });
})
.RequireAuthorization("AdminOnly")
.WithName("CleanupTokens");
```

### Step 6: Add Health Check

Create: `src/Infrastructure/Platform.Infrastructure/HealthChecks/TokenCleanupHealthCheck.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Platform.Persistence.Context;

namespace Platform.Infrastructure.HealthChecks;

public class TokenCleanupHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public TokenCleanupHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check count of expired tokens
            var expiredCount = await dbContext.Set<RefreshToken>()
                .Where(t => t.ExpiresAt < DateTime.UtcNow || t.IsRevoked)
                .CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["expiredTokenCount"] = expiredCount,
                ["threshold"] = 10000
            };

            if (expiredCount > 10000)
            {
                return HealthCheckResult.Degraded(
                    $"High number of expired tokens: {expiredCount}",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Expired tokens: {expiredCount}",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Token cleanup health check failed",
                ex);
        }
    }
}
```

## Verification

```bash
# Check background service logs
docker logs vibexlearn-api 2>&1 | grep "Token cleanup"

# Manual cleanup via admin endpoint
curl -X POST http://localhost:8080/api/v1/auth/cleanup-tokens \
  -H "Authorization: Bearer <admin-token>"

# Response: {"deletedCount": 123}
```

## Priority

**MEDIUM-TERM** - Performance improvement, not security critical.
