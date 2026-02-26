---
name: pagination-limits
description: Missing Pagination Max Limit. `pageSize=int.MaxValue` allows tüm tabloyu çekilebilir. Bu skill adds max limit validation to PageRequest.
---

# Add Pagination Max Limit

## Problem

**Risk Level:** MEDIUM

`PageRequest` allows unlimited `pageSize`:
- `pageSize=int.MaxValue` could cause memory exhaustion
- Attacker could fetch entire table
- Performance degradation

**Affected Files:**
- `src/Core/Platform.Application/Common/Models/Pagination/PageRequest.cs`
- `src/Presentation/Platform.WebAPI/Endpoints/*.cs`

## Solution Steps

### Step 1: Update PageRequest with Max Limit

```csharp
namespace Platform.Application.Common.Models.Pagination;

public sealed record PageRequest
{
    private int _pageSize = 20;

    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init
        {
            // Enforce maximum limit
            var normalized = Math.Min(value, 100);
            _pageSize = Math.Max(1, normalized);
        }
    }

    public string? Sort { get; init; }
    public string? Search { get; init; }

    /// <summary>
    /// Normalized request with enforced limits.
    /// </summary>
    public PageRequest Normalize() => this with
    {
        Page = Math.Max(1, Page),
        PageSize = Math.Max(1, Math.Min(100, PageSize)),
        Search = Search?.Trim()
    };
}
```

### Step 2: Add Endpoint Validation

```csharp
// In endpoint handlers
if (request.PageSize > 100)
{
    return Results.BadRequest(new
    {
        error = "PageSize cannot exceed 100",
        maximum = 100,
        actual = request.PageSize
    });
}
```

### Step 3: Add Pagination Validation Behavior

```csharp
using FluentValidation;

public class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");
    }
}
```

### Step 4: Register Validator
```csharp
services.AddScoped<PageRequestValidator>();
```

### Step 5: Add Validation Pipeline Behavior
```csharp
services.AddTransient(typeof(IPipelineBehavior<, PaginationValidationBehavior>());
```

## Verification
```bash
# Test with invalid page size
curl "http://localhost:8080/api/v1/courses?pageSize=1000"

# Expected: 400 Bad Request

# Test with valid page size
curl "http://localhost:8080/api/v1/courses?pageSize=50"

# Expected: 200 OK
```

## Priority
**SHORT-TERM** - DoS prevention.
