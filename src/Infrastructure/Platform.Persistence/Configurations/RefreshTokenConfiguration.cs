using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.Property(r => r.Token).IsRequired().HasMaxLength(500);
        builder.Property(r => r.ReplacedByToken).HasMaxLength(500);

        builder.HasIndex(r => r.Token)
            .IsUnique().HasDatabaseName("ix_refresh_tokens_token");
        builder.HasIndex(r => new { r.UserId, r.IsRevoked })
            .HasDatabaseName("ix_refresh_tokens_user_revoked");

        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
