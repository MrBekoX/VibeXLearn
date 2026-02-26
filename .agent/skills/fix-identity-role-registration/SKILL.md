---
name: fix-identity-role-registration
description: Identity DI'da IdentityRole<Guid> kullanılıyor, AppRole değil. AppRole'a eklenen custom property'ler (IsActive, CreatedAt) Identity pipeline'da yok sayılıyor. Bu skill, doğru rol tipini register eder.
---

# Fix Identity Role Registration

## Problem

**Risk Level:** HIGH

`AddIdentity<AppUser, IdentityRole<Guid>>` kullanılıyor ama `AppRole` entity'si `IdentityRole<Guid>`'den türüyor ve custom property'ler (`IsActive`, `CreatedAt`) ekliyor.

```csharp
// MEVCUT — YANLIŞ:
services.AddIdentity<AppUser, IdentityRole<Guid>>(opt => { ... })

// AppRole entity'si custom property'ler içeriyor ama
// DI'a IdentityRole<Guid> olarak register edildiği için
// RoleManager<AppRole> inject edilemiyor
```

**Affected Files:**
- `src/Infrastructure/Platform.Persistence/Extensions/PersistenceServiceExtensions.cs`
- `src/Core/Platform.Domain/Entities/AppRole.cs`

## Solution Steps

### Step 1: Verify AppRole Entity

Check `src/Core/Platform.Domain/Entities/AppRole.cs`:

```csharp
using Microsoft.AspNetCore.Identity;

namespace Platform.Domain.Entities;

/// <summary>
/// Application role with additional properties.
/// </summary>
public sealed class AppRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<AppUserRole> UserRoles { get; set; } = [];
}
```

### Step 2: Fix Identity Registration

Modify `src/Infrastructure/Platform.Persistence/Extensions/PersistenceServiceExtensions.cs`:

```csharp
public static IServiceCollection AddPersistence(
    this IServiceCollection services,
    IConfiguration config)
{
    // ... existing code ...

    // CORRECT: Use AppRole instead of IdentityRole<Guid>
    services.AddIdentity<AppUser, AppRole>(opt =>
    {
        // Password settings
        opt.Password.RequireDigit = true;
        opt.Password.RequireLowercase = true;
        opt.Password.RequireUppercase = true;
        opt.Password.RequireNonAlphanumeric = true;
        opt.Password.RequiredLength = 8;

        // Lockout settings
        opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        opt.Lockout.MaxFailedAccessAttempts = 5;
        opt.Lockout.AllowedForNewUsers = true;

        // User settings
        opt.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // Add RoleManager for AppRole
    services.AddScoped<RoleManager<AppRole>>();

    return services;
}
```

### Step 3: Update AppUserRole Junction Table

Ensure junction table uses correct types:

```csharp
// src/Core/Platform.Domain/Entities/AppUserRole.cs
using Microsoft.AspNetCore.Identity;

namespace Platform.Domain.Entities;

public sealed class AppUserRole : IdentityUserRole<Guid>
{
    public AppUser User { get; set; } = null!;
    public AppRole Role { get; set; } = null!;
}
```

### Step 4: Update AppUser Navigation

```csharp
// src/Core/Platform.Domain/Entities/AppUser.cs
public sealed class AppUser : IdentityUser<Guid>
{
    // ... existing properties ...

    // Navigation properties
    public ICollection<AppUserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
```

### Step 5: Update DbContext

Ensure `AppDbContext` uses correct types:

```csharp
// src/Infrastructure/Platform.Persistence/Context/AppDbContext.cs
public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid,
    IdentityUserClaim<Guid>, AppUserRole, IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    // ... existing code ...
}
```

### Step 6: Update Role Seeding

Update seed data to use `AppRole`:

```csharp
// src/Infrastructure/Platform.Persistence/Data/RoleConfiguration.cs
public class RoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.HasData(
            new AppRole
            {
                Id = Guid.Parse("..."),
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "System administrator",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1)
            },
            new AppRole
            {
                Id = Guid.Parse("..."),
                Name = "Instructor",
                NormalizedName = "INSTRUCTOR",
                Description = "Course instructor",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1)
            },
            new AppRole
            {
                Id = Guid.Parse("..."),
                Name = "Student",
                NormalizedName = "STUDENT",
                Description = "Course student",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1)
            }
        );
    }
}
```

### Step 7: Use RoleManager in Services

Now you can inject `RoleManager<AppRole>`:

```csharp
public class RoleService(
    RoleManager<AppRole> roleManager,
    ILogger<RoleService> logger)
{
    public async Task<Result<AppRole>> CreateRoleAsync(
        string name,
        string? description,
        CancellationToken ct)
    {
        var existingRole = await roleManager.FindByNameAsync(name);
        if (existingRole is not null)
            return Result.Fail<AppRole>("ROLE_EXISTS", "Role already exists");

        var role = new AppRole
        {
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Fail<AppRole>("ROLE_CREATE_FAILED", errors);
        }

        logger.LogInformation("Role created: {Name}", name);
        return Result.Success(role);
    }

    public async Task<Result> DeactivateRoleAsync(Guid roleId, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return Result.Fail("ROLE_NOT_FOUND", "Role not found");

        role.IsActive = false;
        var result = await roleManager.UpdateAsync(role);

        return result.Succeeded
            ? Result.Success()
            : Result.Fail("ROLE_UPDATE_FAILED", "Failed to deactivate role");
    }
}
```

## Verification

```bash
# Build and run
dotnet build

# Test role creation
curl -X POST http://localhost:8080/api/v1/admin/roles \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"TestRole","description":"Test role"}'

# Should return created role with IsActive=true
```

## Priority

**SHORT-TERM** - Required for role management features.
