using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    // Seed için deterministic GUID
    internal static readonly Guid BackendId  = Guid.Parse("11111111-0000-0000-0000-000000000001");
    internal static readonly Guid FrontendId = Guid.Parse("11111111-0000-0000-0000-000000000002");
    internal static readonly Guid DevOpsId   = Guid.Parse("11111111-0000-0000-0000-000000000003");

    private static readonly DateTime SeedDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("ix_categories_slug");

        // Self-referencing hiyerarşi
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasQueryFilter(c => !c.IsDeleted);

        // Seed Data — anonymous types required because Category has private constructor
        builder.HasData(
            new { Id = BackendId, Name = "Backend Development",
                Slug = "backend-development",
                Description = (string?)".NET, Node.js, Python ve daha fazlası",
                CreatedAt = SeedDate, ParentId = (Guid?)null,
                UpdatedAt = (DateTime?)null, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedById = (Guid?)null, UpdatedById = (Guid?)null },
            new { Id = FrontendId, Name = "Frontend Development",
                Slug = "frontend-development",
                Description = (string?)"React, Vue, Angular ve modern web teknolojileri",
                CreatedAt = SeedDate, ParentId = (Guid?)null,
                UpdatedAt = (DateTime?)null, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedById = (Guid?)null, UpdatedById = (Guid?)null },
            new { Id = DevOpsId, Name = "DevOps & Cloud",
                Slug = "devops-cloud",
                Description = (string?)"Docker, Kubernetes, AWS ve CI/CD pipeline'ları",
                CreatedAt = SeedDate, ParentId = (Guid?)null,
                UpdatedAt = (DateTime?)null, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedById = (Guid?)null, UpdatedById = (Guid?)null }
        );
    }
}
