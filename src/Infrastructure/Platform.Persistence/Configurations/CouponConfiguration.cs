using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        // FIXED: Soft delete query filter - global filter for all queries
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.Property(c => c.DiscountAmount).HasPrecision(10, 2);

        builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("ix_coupons_code");
    }
}
