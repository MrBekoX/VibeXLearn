using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Persistence.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    internal static readonly Guid DemoCourseId = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static readonly DateTime SeedDate = new(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(5000);
        builder.Property(c => c.ThumbnailUrl).HasMaxLength(500);

        // decimal → numeric(10,2)
        builder.Property(c => c.Price).HasPrecision(10, 2);

        // enum → string
        builder.Property(c => c.Level).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("ix_courses_slug");
        builder.HasIndex(c => new { c.Status, c.CategoryId })
            .HasDatabaseName("ix_courses_status_category");
        builder.HasIndex(c => c.InstructorId).HasDatabaseName("ix_courses_instructor");

        builder.HasOne(c => c.Instructor)
            .WithMany()
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Category)
            .WithMany(cat => cat.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);

        // Seed Data — anonymous type required because Course has private constructor
        builder.HasData(new
        {
            Id              = DemoCourseId,
            Title           = "Clean Architecture ile .NET 10",
            Slug            = "clean-architecture-dotnet-10",
            Description     = (string?)"Onion Architecture, CQRS, MediatR ve production-ready backend.",
            ThumbnailUrl    = (string?)null,
            Price           = 499.00m,
            Level           = CourseLevel.Intermediate,
            Status          = CourseStatus.Published,
            CategoryId      = CategoryConfiguration.BackendId,
            InstructorId    = AdminUserConfiguration.AdminId,
            EnrollmentCount = 0,
            CreatedAt       = SeedDate,
            UpdatedAt       = (DateTime?)null,
            IsDeleted       = false,
            DeletedAt       = (DateTime?)null,
            CreatedById     = (Guid?)null,
            UpdatedById     = (Guid?)null
        });
    }
}
