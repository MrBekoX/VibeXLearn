---
name: enum-validation
description: Enum.Parse endpoint'te unvalidated. Geçersiz enum değeri ArgumentException fırlatır → 500 Internal Server Error. Client'a 400 dönmesi gerekir. Bu skill, güvenli enum parsing ekler.
---

# Add Enum Validation to Endpoints

## Problem

**Risk Level:** HIGH

Endpoint'lerde `Enum.Parse<T>` kullanılıyor ve geçersiz değerler `ArgumentException` fırlatıyor. Bu exception GlobalExceptionMiddleware tarafından 500 olarak dönülüyor.

```csharp
// MEVCUT — SAVUNMASIZ:
Enum.Parse<CourseLevel>(dto.Level, ignoreCase: true),
// "InvalidLevel" gönderilirse → ArgumentException → 500 Internal Server Error
```

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Endpoints/CourseEndpoints.cs`
- `src/Presentation/Platform.WebAPI/Endpoints/OrderEndpoints.cs`
- Other endpoints using Enum.Parse

## Solution Steps

### Step 1: Replace Enum.Parse with Enum.TryParse

Modify `src/Presentation/Platform.WebAPI/Endpoints/CourseEndpoints.cs`:

```csharp
// Before (unsafe)
var level = Enum.Parse<CourseLevel>(dto.Level, ignoreCase: true);

// After (safe)
if (!Enum.TryParse<CourseLevel>(dto.Level, true, out var level))
{
    var validValues = string.Join(", ", Enum.GetNames<CourseLevel>());
    return Results.BadRequest(new
    {
        error = "Invalid course level",
        field = "level",
        value = dto.Level,
        validValues = validValues
    });
}
```

### Step 2: Create SafeEnumParse Helper Method

Create: `src/Presentation/Platform.WebAPI/Helpers/EnumHelper.cs`

```csharp
namespace Platform.WebAPI.Helpers;

/// <summary>
/// Helper methods for safe enum parsing in endpoints.
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Safely parses an enum value and returns a Problem result if invalid.
    /// </summary>
    public static object? TryParseEnum<TEnum>(
        string? value,
        string fieldName,
        out TEnum result) where TEnum : struct, Enum
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return new
            {
                error = $"{fieldName} is required",
                field = fieldName.ToLowerInvariant()
            };
        }

        if (Enum.TryParse<TEnum>(value, true, out var parsed))
        {
            result = parsed;
            return null; // Success
        }

        // Return error object
        var validValues = string.Join(", ", Enum.GetNames<TEnum>());
        return new
        {
            error = $"Invalid {fieldName.ToLowerInvariant()}",
            field = fieldName.ToLowerInvariant(),
            value = value,
            validValues = validValues
        };
    }

    /// <summary>
    /// Gets all valid values for an enum type as comma-separated string.
    /// </summary>
    public static string GetValidValues<TEnum>() where TEnum : Enum
    {
        return string.Join(", ", Enum.GetNames<TEnum>());
    }
}
```

### Step 3: Use Helper in Endpoints

```csharp
// In CourseEndpoints.cs
group.MapPost("/", async (
    CreateCourseRequestDto dto,
    IMediator mediator,
    CancellationToken ct) =>
{
    // Validate CourseLevel
    var levelError = EnumHelper.TryParseEnum<CourseLevel>(
        dto.Level, "Level", out var level);

    if (levelError is not null)
        return Results.BadRequest(levelError);

    // Validate CourseStatus (if provided)
    if (!string.IsNullOrEmpty(dto.Status))
    {
        var statusError = EnumHelper.TryParseEnum<CourseStatus>(
            dto.Status, "Status", out var status);

        if (statusError is not null)
            return Results.BadRequest(statusError);
    }

    var command = new CreateCourseCommand(
        dto.Title,
        dto.Slug,
        dto.Description,
        dto.Price,
        level,  // Safe parsed value
        dto.CategoryId);

    var result = await mediator.Send(command, ct);

    return result.IsSuccess
        ? Results.Created($"/api/v1/courses/{result.Value}", result.Value)
        : Results.BadRequest(new { error = result.Error.Message });
});
```

### Step 4: Add FluentValidation for Enums

Create: `src/Core/Platform.Application/Common/Validators/EnumValidator.cs`

```csharp
using FluentValidation;

namespace Platform.Application.Common.Validators;

/// <summary>
/// Extension methods for enum validation in FluentValidation.
/// </summary>
public static class EnumValidatorExtensions
{
    /// <summary>
    /// Validates that the string value is a valid enum value.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> IsEnumName<T, TEnum>(
        this IRuleBuilder<T, string?> ruleBuilder,
        bool caseSensitive = false)
        where TEnum : struct, Enum
    {
        return ruleBuilder
            .Must((root, value, context) =>
            {
                if (string.IsNullOrEmpty(value))
                    return true; // Use .NotEmpty() for required validation

                return Enum.TryParse<TEnum>(value, !caseSensitive, out _);
            })
            .WithMessage((root, value) =>
            {
                var validValues = string.Join(", ", Enum.GetNames<TEnum>());
                return $"'{{PropertyName}}' must be one of: {validValues}. You provided: '{value}'";
            });
    }
}
```

### Step 5: Use in Validators

```csharp
// In CreateCourseCommandValidator.cs
public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Level)
            .NotEmpty()
            .IsEnumName<CourseLevel>()  // Uses extension method
            .WithMessage("Level must be one of: Beginner, Intermediate, Advanced");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);
    }
}
```

### Step 6: Add Enum DTO Attributes

Create: `src/Core/Platform.Application/Common/Attributes/ValidEnumAttribute.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Platform.Application.Common.Attributes;

/// <summary>
/// Validates that a string is a valid enum value.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ValidEnumAttribute : ValidationAttribute
{
    private readonly Type _enumType;

    public ValidEnumAttribute(Type enumType)
    {
        _enumType = enumType;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "")
            return ValidationResult.Success; // Use [Required] for required validation

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return ValidationResult.Success;

        if (Enum.GetNames(_enumType).Any(name =>
            string.Equals(name, stringValue, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationResult.Success;
        }

        var validValues = string.Join(", ", Enum.GetNames(_enumType));
        return new ValidationResult(
            $"Invalid value for {validationContext.DisplayName}. Valid values: {validValues}");
    }
}
```

### Step 7: Apply to DTOs

```csharp
public record CreateCourseCommandDto
{
    [Required]
    [StringLength(200)]
    public required string Title { get; init; }

    [Required]
    [ValidEnum(typeof(CourseLevel))]
    public required string Level { get; init; }
}
```

## Verification

```bash
# Test invalid enum value
curl -X POST http://localhost:8080/api/v1/courses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"title":"Test","level":"InvalidLevel","price":100}'

# Expected: 400 Bad Request
# {
#   "error": "Invalid course level",
#   "field": "level",
#   "value": "InvalidLevel",
#   "validValues": "Beginner, Intermediate, Advanced"
# }

# Test valid enum value
curl -X POST http://localhost:8080/api/v1/courses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"title":"Test","level":"Beginner","price":100}'

# Expected: 201 Created
```

## Priority

**SHORT-TERM** - Improves API error handling.
