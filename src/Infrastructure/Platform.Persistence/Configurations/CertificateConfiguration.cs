using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("certificates");

        // FIXED: Soft delete query filter - global filter for all queries
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.SertifierCertId).HasMaxLength(200);
        builder.Property(c => c.PublicUrl).HasMaxLength(500);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        // UserId + CourseId unique
        builder.HasIndex(c => new { c.UserId, c.CourseId })
            .IsUnique().HasDatabaseName("ix_certificates_user_course");

        builder.HasOne(c => c.User)
            .WithMany(u => u.Certificates)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Course)
            .WithMany()
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
