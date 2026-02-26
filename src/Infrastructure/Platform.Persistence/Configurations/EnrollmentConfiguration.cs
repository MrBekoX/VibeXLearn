using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");

        // FIXED: Soft delete query filter - global filter for all queries
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Progress).HasPrecision(5, 2);

        // Bir kullanıcı aynı kursa bir kez kayıt olabilir
        builder.HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique().HasDatabaseName("ix_enrollments_user_course");
        builder.HasIndex(e => e.CourseId).HasDatabaseName("ix_enrollments_course");

        builder.HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
