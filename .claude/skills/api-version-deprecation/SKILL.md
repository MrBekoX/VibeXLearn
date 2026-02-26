---
name: api-version-deprecation
description: API version policy yok. Eski versiyonlar süresiz açık kalabilir. Bu skill, API versioning sunset policy ekler.
---

# Add API Version Deprecation Strategy

## Problem

**Risk Level:** MEDIUM

- API version policy yok
- Eski versiyonlar süresiz açık kalabilir
- Client'lar upgrade için uyarılmıyor
- Breaking changes yönetilemiyor

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Extensions/ApiVersioningExtensions.cs`
- `src/Presentation/Platform.WebAPI/Program.cs`

## Solution Steps

### Step 1: Install API Versioning Package

```bash
cd src/Presentation/Platform.WebAPI
dotnet add package Asp.Versioning.Http
dotnet add package Asp.Versioning.Mvc
```

### Step 2: Create ApiVersioningExtensions

Create: `src/Presentation/Platform.WebAPI/Extensions/ApiVersioningExtensions.cs`

```csharp
using Asp.Versioning;
using Asp.Versioning.Conventions;

namespace Platform.WebAPI.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningWithDeprecation(
        this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Default version
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true; // Returns supported versions in headers

            // Versioning schemes
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version")
            );

            // Sunset policy for deprecated versions
            options.Policies.Sunset(new ApiVersion(1, 0))
                .Effective(DateTimeOffset.UtcNow.AddMonths(6)) // Deprecated in 6 months
                .Link("/docs/api/migration-v1-to-v2")
                    .Title("Migration Guide V1 to V2")
                    .Type("text/html");
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
```

### Step 3: Register in Program.cs

```csharp
// Add API versioning with deprecation support
builder.Services.AddApiVersioningWithDeprecation();
```

### Step 4: Version Your Endpoints

Update endpoint groups:

```csharp
// In Program.cs
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

// Version 1 endpoints
var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(1, 0);

v1.MapGet("/courses", async (IMediator mediator, CancellationToken ct) =>
{
    // V1 implementation
});

// Version 2 endpoints (with new features)
var v2 = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(2, 0);

v2.MapGet("/courses", async (IMediator mediator, CancellationToken ct) =>
{
    // V2 implementation with enhanced features
});
```

### Step 5: Add Deprecation Headers Middleware

Create: `src/Infrastructure/Platform.Infrastructure/Middlewares/ApiDeprecationMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Asp.Versioning;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Adds deprecation headers to responses for deprecated API versions.
/// </summary>
public class ApiDeprecationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiDeprecationMiddleware> _logger;
    private readonly IApiVersioningPolicyProvider _policyProvider;

    public ApiDeprecationMiddleware(
        RequestDelegate next,
        ILogger<ApiDeprecationMiddleware> logger,
        IApiVersioningPolicyProvider policyProvider)
    {
        _next = next;
        _logger = logger;
        _policyProvider = policyProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Check if version is deprecated
        var version = context.GetRequestedApiVersion();
        if (version is null) return;

        var policy = await _policyProvider.GetPolicyAsync(version);
        if (policy?.Sunset is not null)
        {
            // Add Sunset header
            context.Response.Headers["Sunset"] = policy.Sunset.Effective.ToString("R");

            // Add Link header for migration guide
            if (policy.Sunset.Links.Count > 0)
            {
                var links = string.Join(", ",
                    policy.Sunset.Links.Select(l =>
                        $"<{l.LinkTarget}>; rel=\"sunset\"; title=\"{l.Title}\"; type=\"{l.Type}\""));

                context.Response.Headers["Link"] = links;
            }

            // Add Deprecation header (boolean)
            context.Response.Headers["Deprecation"] = "true";

            _logger.LogInformation(
                "Deprecated API version {Version} accessed. Sunset: {Sunset}, Path: {Path}",
                version, policy.Sunset.Effective, context.Request.Path);
        }
    }
}
```

### Step 6: Add Version-Specific Swagger Docs

Update Swagger configuration:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // V1 Documentation
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VibeXLearn API v1",
        Version = "v1",
        Description = "DEPRECATED - Will be sunset on " +
            DateTimeOffset.UtcNow.AddMonths(6).ToString("yyyy-MM-dd") +
            ". Please migrate to v2.",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "api@vibexlearn.com"
        }
    });

    // V2 Documentation (current)
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "VibeXLearn API v2",
        Version = "v2",
        Description = "Current stable version of the API."
    });
});

// Configure Swagger UI for multiple versions
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "v2 (Current)");
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1 (Deprecated)");
});
```

### Step 7: Add Version Header to All Responses

```csharp
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var version = context.GetRequestedApiVersion();
        if (version is not null)
        {
            context.Response.Headers["X-Api-Version"] = version.ToString();
        }
        return Task.CompletedTask;
    });

    await next();
});
```

### Step 8: Create Migration Documentation

Create: `docs/api/migration-v1-to-v2.md`

```markdown
# API Migration Guide: V1 to V2

## Sunset Date
V1 will be sunset on: **[Date]**

## Breaking Changes

### 1. Course Endpoint Response Format
- V1: `GET /api/v1/courses` returns array
- V2: `GET /api/v2/courses` returns paginated result with metadata

### 2. Authentication Headers
- V1: `Authorization: Bearer <token>`
- V2: Same, but includes `X-Api-Version: 2.0` header recommended

### 3. Error Response Format
- V1: `{ "error": "message" }`
- V2: `{ "type": "ErrorType", "message": "message", "correlationId": "..." }`

## Migration Steps

1. Update API base URL to use `/api/v2/`
2. Update response parsing for paginated endpoints
3. Update error handling for new error format
4. Test all endpoints with v2

## Support
For migration support, contact: api@vibexlearn.com
```

## Verification

```bash
# Test V1 endpoint with deprecation headers
curl -I http://localhost:8080/api/v1/courses

# Expected headers:
# Sunset: [Date]
# Deprecation: true
# Link: </docs/api/migration-v1-to-v2>; rel="sunset"; title="Migration Guide V1 to V2"
# X-Api-Version: 1.0

# Test V2 endpoint (no deprecation)
curl -I http://localhost:8080/api/v2/courses

# Expected: No deprecation headers
```

## Priority

**MEDIUM-TERM** - API lifecycle management.
