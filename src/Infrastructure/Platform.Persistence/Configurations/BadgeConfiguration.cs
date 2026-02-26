using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    private static readonly Guid FirstEnrollId   = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid FirstCompleteId = Guid.Parse("44444444-0000-0000-0000-000000000002");
    private static readonly DateTime SeedDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("badges");

        // FIXED: Soft delete query filter - global filter for all queries
        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Description).IsRequired().HasMaxLength(500);
        builder.Property(b => b.IconUrl).IsRequired().HasMaxLength(500);

        // JSONB
        builder.Property(b => b.Criteria).IsRequired().HasColumnType("jsonb");

        // Seed Data — anonymous types required because Badge has private constructor
        builder.HasData(
            new { Id = FirstEnrollId,
                Name = "İlk Adım", Description = "İlk kursuna kayıt oldun!",
                IconUrl = "/badges/first-enroll.svg",
                Criteria = """{"type":"enrollment_count","threshold":1}""",
                CreatedAt = SeedDate, UpdatedAt = (DateTime?)null,
                IsDeleted = false, DeletedAt = (DateTime?)null },
            new { Id = FirstCompleteId,
                Name = "Mezun", Description = "İlk kursunu tamamladın!",
                IconUrl = "/badges/first-complete.svg",
                Criteria = """{"type":"completion_count","threshold":1}""",
                CreatedAt = SeedDate, UpdatedAt = (DateTime?)null,
                IsDeleted = false, DeletedAt = (DateTime?)null }
        );
    }
}
