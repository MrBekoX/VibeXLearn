---
name: domain-exception-http-mapping
description: Map DomainException to appropriate HTTP status codes instead of always returning 500.
---

# Domain Exception HTTP Mapping

Map DomainException to appropriate HTTP status codes for better API responses.

## Problem

```csharp
// ❌ BAD: All exceptions return 500
public async Task InvokeAsync(HttpContext ctx)
{
    try { await next(ctx); }
    catch (Exception ex)
    {
        ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        // ...
    }
}
// Result: Validation errors return 500 instead of 400
```

## Solution

```csharp
// ✅ GOOD: Map exceptions to appropriate status codes
public async Task InvokeAsync(HttpContext ctx)
{
    try
    {
        await next(ctx);
    }
    catch (DomainException ex)
    {
        await HandleDomainExceptionAsync(ctx, ex);
    }
    catch (Exception ex)
    {
        await HandleUnknownExceptionAsync(ctx, ex);
    }
}

private static async Task HandleDomainExceptionAsync(HttpContext ctx, DomainException ex)
{
    var (statusCode, errorCode) = ex.Code switch
    {
        // Validation errors
        var c when c.StartsWith("VALIDATION") => (StatusCodes.Status400BadRequest, ex.Code),
        var c when c.Contains("INVALID") => (StatusCodes.Status400BadRequest, ex.Code),
        
        // Authentication/Authorization
        var c when c.StartsWith("AUTH") => (StatusCodes.Status401Unauthorized, ex.Code),
        var c when c.Contains("UNAUTHORIZED") => (StatusCodes.Status401Unauthorized, ex.Code),
        var c when c.Contains("FORBIDDEN") => (StatusCodes.Status403Forbidden, ex.Code),
        
        // Not found
        var c when c.Contains("NOT_FOUND") => (StatusCodes.Status404NotFound, ex.Code),
        var c when c.EndsWith("_NOT_FOUND") => (StatusCodes.Status404NotFound, ex.Code),
        
        // Conflicts
        var c when c.Contains("ALREADY_EXISTS") => (StatusCodes.Status409Conflict, ex.Code),
        var c when c.Contains("CONFLICT") => (StatusCodes.Status409Conflict, ex.Code),
        var c when c.Contains("ALREADY_") => (StatusCodes.Status409Conflict, ex.Code),
        
        // Business rule violations
        var c when c.StartsWith("ORDER") => (StatusCodes.Status422UnprocessableEntity, ex.Code),
        var c when c.StartsWith("COURSE") => (StatusCodes.Status422UnprocessableEntity, ex.Code),
        var c when c.StartsWith("COUPON") => (StatusCodes.Status422UnprocessableEntity, ex.Code),
        
        // Default
        _ => (StatusCodes.Status400BadRequest, ex.Code)
    };

    ctx.Response.StatusCode = statusCode;
    ctx.Response.ContentType = "application/json";

    await ctx.Response.WriteAsJsonAsync(new
    {
        Type = errorCode,
        Message = ex.Message,
        CorrelationId = ctx.TraceIdentifier,
        Timestamp = DateTime.UtcNow
    });
}

private static async Task HandleUnknownExceptionAsync(HttpContext ctx, Exception ex)
{
    // Log the full exception
    logger.LogError(ex, "Unhandled exception. CorrelationId: {Id}", ctx.TraceIdentifier);

    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    ctx.Response.ContentType = "application/json";

    await ctx.Response.WriteAsJsonAsync(new
    {
        Type = "InternalServerError",
        Message = "An unexpected error occurred.",
        CorrelationId = ctx.TraceIdentifier,
        Timestamp = DateTime.UtcNow
    });
}
```

## Domain Exception Extensions

```csharp
// Add HttpStatusCode property to DomainException
public class DomainException : Exception
{
    public string Code { get; }
    public int? HttpStatusCode { get; init; }

    public DomainException(string code, string message, int? httpStatusCode = null) 
        : base(message)
    {
        Code = code;
        HttpStatusCode = httpStatusCode;
    }

    // Factory methods for common cases
    public static DomainException NotFound(string entity, Guid id) 
        => new($"{entity.ToUpper()}_NOT_FOUND", $"{entity} with id {id} not found.", 404);

    public static DomainException Validation(string message) 
        => new("VALIDATION_ERROR", message, 400);

    public static DomainException Conflict(string message) 
        => new("CONFLICT", message, 409);

    public static DomainException Unauthorized(string message) 
        => new("AUTH_UNAUTHORIZED", message, 401);
}
```

## Mapping Configuration

```csharp
// appsettings.json - Customizable mapping
{
  "ExceptionMapping": {
    "AUTH_INVALID_CREDENTIALS": 401,
    "AUTH_ACCOUNT_LOCKED": 403,
    "AUTH_EMAIL_EXISTS": 409,
    "USER_NOT_FOUND": 404,
    "COURSE_PUBLISH_INVALID_STATUS": 422,
    "ORDER_COUPON_INVALID": 422,
    "COUPON_EXPIRED": 400
  }
}
```

```csharp
// Load from configuration
public class ExceptionMappingOptions
{
    public Dictionary<string, int> Mappings { get; set; } = new();
}

var mapping = config.GetSection("ExceptionMapping").Get<ExceptionMappingOptions>();
var statusCode = mapping.Mappings.GetValueOrDefault(ex.Code, 400);
```

## Usage in Domain

```csharp
public void Publish()
{
    if (Status != CourseStatus.Draft)
        throw DomainException.InvalidOperation(
            "COURSE_PUBLISH_INVALID_STATUS",
            "Only draft courses can be published.");
    
    Status = CourseStatus.Published;
}
```

## Best Practices

1. **Don't expose internal details** in production
2. **Log full exceptions** on server side
3. **Use consistent error codes** across API
4. **Include correlation ID** for debugging
5. **Document error codes** in API documentation
