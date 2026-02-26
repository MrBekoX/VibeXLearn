---
name: nplus1-query-fix
description: Fix N+1 query problems in Entity Framework Core by using Include, projection, and efficient querying patterns.
---

# N+1 Query Fix

Detect and fix N+1 query problems in Entity Framework Core applications.

## Problem Detection

```csharp
// ❌ BAD: N+1 Query Problem
var users = userManager.Users;  // Query 1: SELECT * FROM users
foreach (var u in users)        
{
    var loadedUser = await userManager.FindByIdAsync(u.Id.ToString());  // Query N
    existingToken = loadedUser.RefreshTokens.FirstOrDefault(...);       // Query N+1
}
// Total queries: 1 + (2 * N) users
```

## Solutions

### Solution 1: Eager Loading with Include

```csharp
// ✅ GOOD: Single query with JOIN
var user = await dbContext.Users
    .Include(u => u.RefreshTokens)
    .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken), ct);

// Generated SQL:
// SELECT ... FROM users 
// LEFT JOIN refresh_tokens ON ...
// WHERE refresh_tokens.token = @p0
```

### Solution 2: Direct Entity Query

```csharp
// ✅ GOOD: Query the related entity directly
var refreshToken = await dbContext.RefreshTokens
    .Include(rt => rt.User)
    .ThenInclude(u => u.Roles)
    .FirstOrDefaultAsync(rt => rt.Token == token && rt.IsActive, ct);

if (refreshToken?.User is null)
    return Result.Fail("AUTH_INVALID_REFRESH_TOKEN", "Invalid token");

var user = refreshToken.User;
```

### Solution 3: Projection

```csharp
// ✅ GOOD: Select only needed fields
var result = await dbContext.Users
    .Where(u => u.RefreshTokens.Any(t => t.Token == refreshToken))
    .Select(u => new 
    {
        User = u,
        Token = u.RefreshTokens.First(t => t.Token == refreshToken)
    })
    .FirstOrDefaultAsync(ct);
```

### Solution 4: Split Queries (for large datasets)

```csharp
// ✅ GOOD: Multiple queries but controlled
var user = await dbContext.Users
    .AsSplitQuery()  // Generates separate queries but efficient
    .Include(u => u.RefreshTokens)
    .Include(u => u.Roles)
    .FirstOrDefaultAsync(u => u.Id == userId, ct);
```

## AuthService Refactoring

```csharp
// FIXED AuthService.cs
public async Task<Result<(string AccessToken, DateTime ExpiresAt, string NewRefreshToken)>> 
    RefreshAsync(string refreshToken, CancellationToken ct)
{
    // Single query to find user by refresh token
    var user = await _dbContext.Users
        .Include(u => u.RefreshTokens)
        .FirstOrDefaultAsync(
            u => u.RefreshTokens.Any(t => t.Token == refreshToken && t.IsActive), 
            ct);

    if (user is null)
        return Result.Fail<(string, DateTime, string)>(
            "AUTH_INVALID_REFRESH_TOKEN", 
            AuthBusinessMessages.InvalidRefreshToken);

    var existingToken = user.RefreshTokens.First(t => t.Token == refreshToken);
    
    // Rotate token
    var newRefreshTokenString = GenerateRefreshTokenString();
    existingToken.RevokeAndReplace(newRefreshTokenString);

    var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenString, RefreshTokenLifetime);
    user.AddRefreshToken(newRefreshToken);

    await _userManager.UpdateAsync(user);

    var roles = await _userManager.GetRolesAsync(user);
    var (accessToken, expiresAt) = GenerateAccessToken(user, roles);

    _logger.LogInformation("{Event} | UserId:{UserId}",
        SecurityAuditEvents.TokenRefreshed, user.Id);

    return Result.Success((accessToken, expiresAt, newRefreshTokenString));
}
```

## Detection Tools

```csharp
// Enable EF Core logging in Development
services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(connectionString);
    opt.EnableSensitiveDataLogging();
    opt.LogTo(Console.WriteLine, LogLevel.Information);
});
```

```bash
# Using dotnet-trace
dotnet tool install --global dotnet-trace
dotnet trace collect --process-id <PID> --providers Microsoft.EntityFrameworkCore
```

## Repository Patterns

```csharp
// Generic optimized read method
public async Task<T?> GetByIdWithIncludesAsync(
    Guid id, 
    CancellationToken ct, 
    params Expression<Func<T, object>>[] includes)
{
    var query = _context.Set<T>().AsQueryable();
    
    foreach (var include in includes)
    {
        query = query.Include(include);
    }
    
    return await query.FirstOrDefaultAsync(e => e.Id == id, ct);
}

// Usage
var user = await _userRepo.GetByIdWithIncludesAsync(
    userId, 
    ct,
    u => u.RefreshTokens,
    u => u.Roles,
    u => u.Enrollments);
```

## Performance Comparison

| Approach | Queries | Suitable For |
|----------|---------|--------------|
| N+1 (BAD) | 1 + (N × M) | Never |
| Include | 1 | Small to medium datasets |
| SplitQuery | 2-3 | Large datasets with many includes |
| Projection | 1 | When only specific fields needed |
| Raw SQL | 1 | Complex queries, reports |

## Best Practices

1. **Always check execution plan** with `.ToQueryString()`
2. **Use `AsNoTracking()`** for read-only scenarios
3. **Avoid cartesian explosion** with many includes
4. **Consider Dapper** for complex read-only queries
5. **Use caching** for frequently accessed data
