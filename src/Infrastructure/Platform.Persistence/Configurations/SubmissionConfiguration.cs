using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

public sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions");

        builder.Property(s => s.RepoUrl).IsRequired().HasMaxLength(500);
        builder.Property(s => s.CommitSha).HasMaxLength(40);
        builder.Property(s => s.Branch).HasMaxLength(200);
        builder.Property(s => s.PrUrl).HasMaxLength(500);
        builder.Property(s => s.ReviewNote).HasMaxLength(2000);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // Bir öğrenci aynı lesson için tek submission
        builder.HasIndex(s => new { s.StudentId, s.LessonId })
            .IsUnique().HasDatabaseName("ix_submissions_student_lesson");

        builder.HasOne(s => s.Student)
            .WithMany(u => u.Submissions)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Lesson)
            .WithMany(l => l.Submissions)
            .HasForeignKey(s => s.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
