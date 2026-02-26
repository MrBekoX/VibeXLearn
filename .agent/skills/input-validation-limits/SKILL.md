---
name: input-validation-limits
description: Search parametrelerinde uzunluk limiti yok. Aşırı uzun search string'ler memory tüketimine yol açabilir. DTO'larda StringLength validation eksik. Bu skill, input length validation ekler.
---

# Add Input Length Limits and Validation

## Problem

**Risk Level:** MEDIUM

1. **Search Parameters Unlimited** - `search` parameter has no length limit
2. **Missing StringLength on DTOs** - DTO'larda explicit string length validation yok
3. **Potential Memory Exhaustion** - Aşırı uzun string'ler memory tüketimine yol açabilir

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Endpoints/CourseEndpoints.cs`
- `src/Core/Platform.Application/Features/*/DTOs/*.cs`
- `src/Core/Platform.Application/Common/Models/Pagination/PageRequest.cs`

## Solution Steps

### Step 1: Add Search Length Limit in Endpoints

Modify `src/Presentation/Platform.WebAPI/Endpoints/CourseEndpoints.cs`:

```csharp
private const int MaxSearchLength = 200;

group.MapGet("/", async (
    IMediator mediator,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? sort = null,
    [FromQuery] string? search = null,
    CancellationToken ct = default) =>
{
    // Validate search length
    if (search?.Length > MaxSearchLength)
    {
        return Results.BadRequest(new
        {
            error = "Search term too long",
            maxLength = MaxSearchLength,
            actualLength = search.Length
        });
    }

    var query = new GetAllCoursesQuery(new PageRequest
    {
        Page = page,
        PageSize = pageSize,
        Sort = sort,
        Search = search
    });

    var result = await mediator.Send(query, ct);
    // ... rest of handler
});
```

### Step 2: Create SearchValidationFilter

Create: `src/Presentation/Platform.WebAPI/Filters/SearchValidationFilter.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Platform.WebAPI.Filters;

/// <summary>
/// Validates search parameter length across all endpoints.
/// </summary>
public class SearchValidationFilter(int maxLength = 200) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;

            // Check string properties
            var stringProperties = arg.GetType()
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string));

            foreach (var prop in stringProperties)
            {
                var value = prop.GetValue(arg) as string;
                if (value?.Length > maxLength)
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        error = $"{prop.Name} exceeds maximum length",
                        maxLength,
                        actualLength = value.Length
                    });
                    return;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
```

### Step 3: Add StringLength Validation to DTOs

Update all DTOs with proper validation attributes:

**Example: `CreateCourseCommandDto.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Platform.Application.Features.Courses.DTOs;

/// <summary>
/// DTO for creating a new course.
/// </summary>
public sealed record CreateCourseCommandDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public required string Title { get; init; }

    [Required(ErrorMessage = "Slug is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Slug must be between 3 and 100 characters")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must be URL-friendly (lowercase, hyphens only)")]
    public required string Slug { get; init; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 5000 characters")]
    public required string Description { get; init; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0, 99999.99, ErrorMessage = "Price must be between 0 and 99,999.99")]
    public decimal Price { get; init; }

    [Required(ErrorMessage = "Level is required")]
    public required string Level { get; init; }

    [Required(ErrorMessage = "CategoryId is required")]
    public Guid CategoryId { get; init; }
}
```

### Step 4: Update PageRequest with Search Validation

Modify `src/Core/Platform.Application/Common/Models/Pagination/PageRequest.cs`:

```csharp
namespace Platform.Application.Common.Models.Pagination;

public sealed record PageRequest
{
    private int _pageSize = 20;

    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value is < 1 or > 100 ? 20 : value;
    }

    private string? _search;
    public string? Search
    {
        get => _search;
        init => _search = value?.Length > 200 ? value[..200] : value;
    }

    public string? Sort { get; init; }

    public static readonly PageRequest Default = new();

    public PageRequest Normalize() => this with
    {
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize < 1 ? 20 : PageSize > 100 ? 100 : PageSize,
        Search = Search?.Trim()
    };
}
```

### Step 5: Add Global Validation Middleware

Create: `src/Infrastructure/Platform.Infrastructure/Middlewares/InputValidationMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Validates request body size and content length.
/// </summary>
public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;
    private const int MaxContentLength = 10 * 1024 * 1024; // 10MB

    public InputValidationMiddleware(
        RequestDelegate next,
        ILogger<InputValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check content length for POST/PUT/PATCH
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            if (context.Request.ContentLength > MaxContentLength)
            {
                _logger.LogWarning(
                    "Request body too large. ContentLength: {Length}, Max: {Max}",
                    context.Request.ContentLength, MaxContentLength);

                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Request body too large",
                    maxSize = "10MB"
                });
                return;
            }
        }

        await _next(context);
    }
}
```

## Verification

```bash
# Test search length limit
curl "http://localhost:8080/api/v1/courses?search=$(python3 -c 'print("a"*300)')"
# Should return 400 with error message

# Test valid search
curl "http://localhost:8080/api/v1/courses?search=valid"
# Should return 200
```

## Priority

**SHORT-TERM** - DoS prevention, but low immediate risk.
