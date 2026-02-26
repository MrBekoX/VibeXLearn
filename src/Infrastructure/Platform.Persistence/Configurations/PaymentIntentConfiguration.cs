using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.ToTable("payment_intents");

        // FIXED: Soft delete query filter - global filter for all queries
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.ConversationId).IsRequired().HasMaxLength(100);
        builder.Property(p => p.IyzicoToken).HasMaxLength(500);
        builder.Property(p => p.IyzicoPaymentId).HasMaxLength(100);
        builder.Property(p => p.ExpectedPrice).HasPrecision(10, 2);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.FailReason).HasMaxLength(500);

        // Audit JSON
        builder.Property(p => p.RawCallbackSnapshot).HasColumnType("text");

        // Iyzico idempotency garantisi
        builder.HasIndex(p => p.ConversationId)
            .IsUnique().HasDatabaseName("ix_payment_intents_conversation_id");

        // 1-1
        builder.HasOne(p => p.Order)
            .WithOne(o => o.PaymentIntent)
            .HasForeignKey<PaymentIntent>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
