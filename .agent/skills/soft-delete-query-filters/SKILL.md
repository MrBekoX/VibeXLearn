---
name: soft-delete-query-filters
description: Soft delete query filter'ları sadece AppUser'da var. Course, Order, Enrollment ve diğer entity'lerde eksik. Silinmiş veriler yanlışlıkla sorgu sonuçlarına dahil olabilir. Bu skill, tüm entity'lere global query filter ekler.
---

# Add Soft Delete Query Filters to All Entities

## Problem

**Risk Level:** MEDIUM

Soft delete query filter'ları sadece `AppUserConfiguration.cs` içinde `HasQueryFilter(u => !u.IsDeleted)` var. Diğer tüm entity'lerde (`Course`, `Order`, `Enrollment`, `Lesson`, vb.) eksik.

**Risk:** Silinmiş veriler yanlışlıkla sorgu sonuçlarına dahil olabilir.

**Affected Files:**
- `src/Infrastructure/Platform.Persistence/Configurations/*.cs`
- `src/Infrastructure/Platform.Persistence/Context/AppDbContext.cs`

## Solution Steps

### Step 1: Add Query Filter to Each Configuration

For each entity configuration file, add the query filter:

**Example: `CourseConfiguration.cs`**

```csharp
public void Configure(EntityTypeBuilder<Course> builder)
{
    builder.ToTable("courses");

    // SOFT DELETE QUERY FILTER - Required for all entities
    builder.HasQueryFilter(c => !c.IsDeleted);

    builder.HasKey(c => c.Id);

    builder.Property(c => c.Title)
        .HasMaxLength(200)
        .IsRequired();

    // ... other configuration
}
```

**Example: `OrderConfiguration.cs`**

```csharp
public void Configure(EntityTypeBuilder<Order> builder)
{
    builder.ToTable("orders");

    // SOFT DELETE QUERY FILTER
    builder.HasQueryFilter(o => !o.IsDeleted);

    // ... other configuration
}
```

### Step 2: List of Entities Requiring Filter

Add `HasQueryFilter(e => !e.IsDeleted)` to all configurations:

| Entity | Configuration File | Status |
|--------|-------------------|--------|
| AppUser | AppUserConfiguration.cs | ✅ Done |
| Course | CourseConfiguration.cs | ❌ Missing |
| Order | OrderConfiguration.cs | ❌ Missing |
| Enrollment | EnrollmentConfiguration.cs | ❌ Missing |
| Lesson | LessonConfiguration.cs | ❌ Missing |
| PaymentIntent | PaymentIntentConfiguration.cs | ❌ Missing |
| Category | CategoryConfiguration.cs | ❌ Missing |
| Badge | BadgeConfiguration.cs | ❌ Missing |
| Coupon | CouponConfiguration.cs | ❌ Missing |
| Submission | SubmissionConfiguration.cs | ❌ Missing |
| LiveSession | LiveSessionConfiguration.cs | ❌ Missing |

### Step 3: Alternative - Global Convention (Recommended)

Create a global convention that automatically applies filter to all entities implementing `ISoftDeletable`:

**Create: `src/Infrastructure/Platform.Persistence/Conventions/SoftDeleteQueryFilterConvention.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Platform.Domain.Common;

namespace Platform.Persistence.Conventions;

/// <summary>
/// Convention that automatically adds soft delete query filter
/// to all entities implementing ISoftDeletable interface.
/// </summary>
public class SoftDeleteQueryFilterConvention : IModelInitializedConvention
{
    public void ProcessModelInitialized(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                entityType.SetQueryFilter(
                    CreateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static LambdaExpression CreateSoftDeleteFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var filter = Expression.Lambda(
            Expression.Not(property),
            parameter);

        return filter;
    }
}
```

**Create marker interface: `src/Core/Platform.Domain/Common/ISoftDeletable.cs`**

```csharp
namespace Platform.Domain.Common;

/// <summary>
/// Marker interface for entities that support soft delete.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}
```

**Register convention in `AppDbContext.cs`:**

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Conventions.Add(_ => new SoftDeleteQueryFilterConvention());
}
```

### Step 4: Update BaseEntity to Implement ISoftDeletable

```csharp
// src/Core/Platform.Domain/Common/BaseEntity.cs
public abstract class BaseEntity : ISoftDeletable
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; internal set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; internal set; }
    public bool IsDeleted { get; internal set; }
    public DateTime? DeletedAt { get; internal set; }
}
```

### Step 5: Bypass Filter When Needed

For admin queries that need to see deleted records:

```csharp
// Use IgnoreQueryFilters for specific queries
var allCoursesIncludingDeleted = await _context.Courses
    .IgnoreQueryFilters()
    .Where(c => c.CategoryId == categoryId)
    .ToListAsync(ct);
```

## Verification

```csharp
// Unit test to verify filter is applied
[Fact]
public async Task SoftDeleted_Course_Should_Not_Be_Returned()
{
    // Arrange
    var course = Course.Create("Test", "test", 100m, CourseLevel.Beginner, Guid.NewGuid(), Guid.NewGuid());
    await _context.Courses.AddAsync(course);
    await _context.SaveChangesAsync();

    // Act - Soft delete
    course.SoftDelete();
    await _context.SaveChangesAsync();

    // Assert - Should not be returned by normal query
    var result = await _context.Courses.FirstOrDefaultAsync(c => c.Id == course.Id);
    result.Should().BeNull();

    // Assert - Should be returned when ignoring filters
    var withDeleted = await _context.Courses
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(c => c.Id == course.Id);
    withDeleted.Should().NotBeNull();
    withDeleted!.IsDeleted.Should().BeTrue();
}
```

## Priority

**SHORT-TERM** - Data integrity concern, but low immediate risk.
