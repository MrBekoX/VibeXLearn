---
name: fix-current-user-service
description: ICurrentUserService interface'i tanımlı ama implementation yok. Bu skill, authentication'ın düzgün çalışması için gerekli CurrentUserService implementation'ını oluşturur. Handler'larda current user bilgisi alınamıyor, authorization çalışmıyor, audit fields set edilemiyor.
---

# Fix ICurrentUserService Implementation

## Problem

**Risk Level:** CRITICAL

`ICurrentUserService` ve `ICurrentUser` interfaceleri tanımlı ama Infrastructure katmanında implementation YOK. Bu olmadan:
- Handler'larda current user bilgisi alınamaz
- Authorization düzgün çalışmaz
- Audit fields (CreatedById, UpdatedById) set edilemez

**Affected Files:**
- `src/Core/Platform.Application/Common/Interfaces/ICurrentUser.cs`
- `src/Core/Platform.Application/Common/Interfaces/ICurrentUserService.cs`

## Solution Steps

### Step 1: Create CurrentUserService Implementation

Create file: `src/Infrastructure/Platform.Infrastructure/Services/CurrentUserService.cs`

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Platform.Application.Common.Interfaces;

namespace Platform.Infrastructure.Services;

/// <summary>
/// HTTP context'ten current user bilgilerini çıkarır.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return null;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var id) ? id : null;
        }
    }

    public string? Email =>
        httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
        ?? httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;

    public string? FirstName =>
        httpContextAccessor.HttpContext?.User?.FindFirst("firstName")?.Value;

    public string? LastName =>
        httpContextAccessor.HttpContext?.User?.FindFirst("lastName")?.Value;

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IList<string> Roles =>
        httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? [];

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}
```

### Step 2: Register in DI

Add to `src/Infrastructure/Platform.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`:

```csharp
// HttpContextAccessor ekle (yoksa)
services.AddHttpContextAccessor();

// CurrentUserService'i register et
services.AddScoped<ICurrentUserService, CurrentUserService>();
```

### Step 3: Update Handler Usage

Handler'larda şu şekilde kullan:

```csharp
public sealed class CreateCourseCommandHandler(
    ICurrentUserService currentUser,
    IWriteRepository<Course> writeRepo,
    IUnitOfWork uow) : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCourseCommand cmd, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Result.Fail<Guid>("AUTH_UNAUTHORIZED", "User not authenticated.");

        var course = Course.Create(
            cmd.Title,
            cmd.Slug,
            cmd.Price,
            cmd.Level,
            currentUser.UserId!.Value,  // Instructor
            cmd.CategoryId);

        course.SetCreatedBy(currentUser.UserId.Value);  // Audit

        await writeRepo.AddAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success(course.Id);
    }
}
```

## Verification

1. Build solution: `dotnet build`
2. Test endpoint: `curl -H "Authorization: Bearer <token>" http://localhost:8080/api/v1/auth/profile`
3. Should return current user info without errors

## Priority

**IMMEDIATE** - Bu olmadan authentication sistemi çalışmıyor.
