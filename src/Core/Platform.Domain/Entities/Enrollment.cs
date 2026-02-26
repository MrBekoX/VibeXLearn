using Platform.Domain.Common;
using Platform.Domain.Enums;
using Platform.Domain.Events;
using Platform.Domain.ValueObjects;

namespace Platform.Domain.Entities;

/// <summary>
/// Enrollment with progress tracking and completion logic.
/// </summary>
public class Enrollment : BaseEntity, IAggregateRoot
{
    // Private setters for encapsulation
    public Guid             UserId      { get; private set; }
    public Guid             CourseId    { get; private set; }
    public EnrollmentStatus Status      { get; private set; } = EnrollmentStatus.Active;
    public DateTime?        CompletedAt { get; private set; }
    public decimal          Progress    { get; private set; } = 0;

    // Navigation properties
    public AppUser          User        { get; private set; } = default!;
    public Course           Course      { get; private set; } = default!;

    // Private constructor for EF Core
    private Enrollment() { }

    /// <summary>
    /// Factory method to create a new enrollment.
    /// </summary>
    public static Enrollment Create(Guid userId, Guid courseId)
    {
        Guard.Against.EmptyGuid(userId, nameof(userId));
        Guard.Against.EmptyGuid(courseId, nameof(courseId));

        var enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            Progress = 0
        };

        enrollment.AddDomainEvent(new EnrollmentCreatedEvent(enrollment.Id, userId, courseId));
        return enrollment;
    }

    /// <summary>
    /// Update progress (0-100). Auto-completes when reaching 100.
    /// </summary>
    public void UpdateProgress(decimal progress)
    {
        if (Status != EnrollmentStatus.Active)
            throw new DomainException("ENROLLMENT_PROGRESS_INVALID_STATUS",
                "Cannot update progress on non-active enrollment.");

        var validatedProgress = ValueObjects.Progress.Of(progress);
        Progress = validatedProgress;
        MarkAsUpdated();

        AddDomainEvent(new EnrollmentProgressUpdatedEvent(Id, Progress));

        // Auto-complete when reaching 100%
        if (validatedProgress.IsCompleted)
        {
            Complete();
        }
    }

    /// <summary>
    /// Mark enrollment as completed.
    /// </summary>
    public void Complete()
    {
        if (Status != EnrollmentStatus.Active)
            throw new DomainException("ENROLLMENT_COMPLETE_INVALID_STATUS",
                "Only active enrollments can be completed.");

        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Progress = 100;
        MarkAsUpdated();

        AddDomainEvent(new EnrollmentCompletedEvent(Id, UserId, CourseId));
    }

    /// <summary>
    /// Cancel enrollment.
    /// </summary>
    public void Cancel()
    {
        if (Status == EnrollmentStatus.Completed)
            throw new DomainException("ENROLLMENT_CANCEL_COMPLETED",
                "Cannot cancel a completed enrollment.");

        Status = EnrollmentStatus.Cancelled;
        MarkAsUpdated();
    }

    /// <summary>
    /// Reactivate cancelled enrollment.
    /// </summary>
    public void Reactivate()
    {
        if (Status != EnrollmentStatus.Cancelled)
            throw new DomainException("ENROLLMENT_REACTIVATE_INVALID_STATUS",
                "Only cancelled enrollments can be reactivated.");

        Status = EnrollmentStatus.Active;
        MarkAsUpdated();
    }

    /// <summary>
    /// Check if enrollment is active.
    /// </summary>
    public bool IsActive => Status == EnrollmentStatus.Active;

    /// <summary>
    /// Check if enrollment is completed.
    /// </summary>
    public bool IsCompleted => Status == EnrollmentStatus.Completed;

    /// <summary>
    /// Check if user has access to course content.
    /// </summary>
    public bool HasAccess => Status == EnrollmentStatus.Active || Status == EnrollmentStatus.Completed;
}
