using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.Bio).HasMaxLength(1000);

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasIndex(u => u.CreatedAt).HasDatabaseName("ix_users_created_at");
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");
    }
}
