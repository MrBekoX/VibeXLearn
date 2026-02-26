---
name: request-size-limits
description: Request size limit konfigürasyonu yok. Large payload attacks mümkün. Bu skill, Kestrel ve form limits ekler.
---

# Add Request Size Limits

## Problem

**Risk Level:** MEDIUM

Request size limit konfigürasyonu yok. Saldırganlar büyük payload'lar göndererek:
- Memory exhaustion
- DoS attacks
- Disk space exhaustion

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Program.cs`

## Solution Steps

### Step 1: Configure Kestrel Limits

Modify `src/Presentation/Platform.WebAPI/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel limits
builder.WebHost.ConfigureKestrel(kestrelOptions =>
{
    kestrelOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    kestrelOptions.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32KB
    kestrelOptions.Limits.MaxRequestHeaderCount = 100;
    kestrelOptions.Limits.MaxRequestLineSize = 8 * 1024; // 8KB
    kestrelOptions.Limits.MaxConcurrentConnections = 100;
    kestrelOptions.Limits.MaxConcurrentUpgradedConnections = 100;
    kestrelOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    kestrelOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});
```

### Step 2: Configure Form Options

Add to `Program.cs`:

```csharp
// Configure form options for multipart uploads
builder.Services.Configure<FormOptions>(formOptions =>
{
    formOptions.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    formOptions.ValueLengthLimit = int.MaxValue; // Max form value length
    formOptions.MultipartHeadersLengthLimit = 32 * 1024; // 32KB
    formOptions.BufferBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});
```

### Step 3: Add RequestSizeLimit Attribute to Endpoints

For specific endpoints with different limits:

```csharp
// For file upload endpoints - larger limit
group.MapPost("/upload", async (IFormFile file, CancellationToken ct) =>
{
    // Handle file upload
})
.WithName("UploadFile")
.WithMetadata(new RequestSizeLimitAttribute(100 * 1024 * 1024)); // 100MB for uploads

// For standard endpoints - smaller limit
group.MapPost("/create", async (CreateCommand cmd, IMediator mediator, CancellationToken ct) =>
{
    // Handle create
})
.WithMetadata(new RequestSizeLimitAttribute(1024 * 1024)); // 1MB for JSON
```

### Step 4: Create Custom Attribute for Common Limits

Create: `src/Presentation/Platform.WebAPI/Attributes/RequestSizeLimits.cs`

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Platform.WebAPI.Attributes;

/// <summary>
/// Common request size limit constants.
/// </summary>
public static class RequestSizeLimits
{
    public const int Small = 100 * 1024;       // 100KB - for simple JSON
    public const int Medium = 1024 * 1024;      // 1MB - for standard requests
    public const int Large = 10 * 1024 * 1024;  // 10MB - for rich content
    public const int FileUpload = 100 * 1024 * 1024; // 100MB - for file uploads
}

/// <summary>
/// Applies small request size limit (100KB).
/// </summary>
public class SmallRequestLimitAttribute : RequestSizeLimitAttribute
{
    public SmallRequestLimitAttribute()
        : base(RequestSizeLimits.Small) { }
}

/// <summary>
/// Applies medium request size limit (1MB).
/// </summary>
public class MediumRequestLimitAttribute : RequestSizeLimitAttribute
{
    public MediumRequestLimitAttribute()
        : base(RequestSizeLimits.Medium) { }
}

/// <summary>
/// Applies large request size limit (10MB).
/// </summary>
public class LargeRequestLimitAttribute : RequestSizeLimitAttribute
{
    public LargeRequestLimitAttribute()
        : base(RequestSizeLimits.Large) { }
}
```

### Step 5: Add Global Request Size Middleware

Create: `src/Infrastructure/Platform.Infrastructure/Middlewares/RequestSizeValidationMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Validates request size early in the pipeline.
/// </summary>
public class RequestSizeValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSizeValidationMiddleware> _logger;
    private readonly long _maxRequestSize;

    public RequestSizeValidationMiddleware(
        RequestDelegate next,
        ILogger<RequestSizeValidationMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _maxRequestSize = config.GetValue("MaxRequestSize", 10 * 1024 * 1024L);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for GET, HEAD, OPTIONS
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check content length
        if (context.Request.ContentLength.HasValue &&
            context.Request.ContentLength > _maxRequestSize)
        {
            _logger.LogWarning(
                "Request rejected: body too large. Size: {Size}, Max: {Max}, Path: {Path}",
                context.Request.ContentLength, _maxRequestSize, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Request body too large",
                maxSize = $"{_maxRequestSize / (1024 * 1024)}MB"
            });
            return;
        }

        await _next(context);
    }
}
```

### Step 6: Register Middleware

Add to `Program.cs`:

```csharp
// Add early in the pipeline
app.UseMiddleware<RequestSizeValidationMiddleware>();
```

## Configuration in appsettings.json

```json
{
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 10485760,
      "MaxRequestHeadersTotalSize": 32768,
      "MaxRequestHeaderCount": 100
    }
  },
  "MaxRequestSize": 10485760
}
```

## Verification

```bash
# Test with oversized request
dd if=/dev/zero bs=1M count=20 | curl -X POST \
  -H "Content-Type: application/json" \
  --data-binary @- \
  http://localhost:8080/api/v1/courses

# Should return 413 Payload Too Large
```

## Priority

**SHORT-TERM** - DoS prevention.
