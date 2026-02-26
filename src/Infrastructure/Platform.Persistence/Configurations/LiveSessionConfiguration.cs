using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.ToTable("live_sessions");

        // FIXED: Soft delete query filter - global filter for all queries
        builder.HasQueryFilter(ls => !ls.IsDeleted);

        builder.Property(ls => ls.Topic).IsRequired().HasMaxLength(300);
        builder.Property(ls => ls.MeetingId).HasMaxLength(100);
        builder.Property(ls => ls.JoinUrl).HasMaxLength(500);
        builder.Property(ls => ls.StartUrl).HasMaxLength(500);
        builder.Property(ls => ls.Status).HasConversion<string>().HasMaxLength(20);

        // 1-1: her lesson'ın tek live session'ı
        builder.HasOne(ls => ls.Lesson)
            .WithOne(l => l.LiveSession)
            .HasForeignKey<LiveSession>(ls => ls.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Partial index
        builder.HasIndex(ls => ls.MeetingId)
            .IsUnique()
            .HasDatabaseName("ix_live_sessions_meeting_id")
            .HasFilter("meeting_id IS NOT NULL");
    }
}
