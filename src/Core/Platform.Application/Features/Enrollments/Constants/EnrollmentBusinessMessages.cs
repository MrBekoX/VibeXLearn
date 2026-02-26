namespace Platform.Application.Features.Enrollments.Constants;

/// <summary>
/// Business messages for Enrollment feature.
/// </summary>
public static class EnrollmentBusinessMessages
{
    public const string NotFound = "Enrollment not found.";
    public const string NotFoundById = "Enrollment not found with the specified ID.";
    public const string AlreadyExists = "You are already enrolled in this course.";
    public const string AlreadyCompleted = "Enrollment is already completed.";
    public const string AlreadyCancelled = "Enrollment has been cancelled.";
    public const string NotActive = "Enrollment is not active.";
    public const string CannotCancelCompleted = "Cannot cancel a completed enrollment.";
    public const string CourseNotPublished = "Course is not published.";
}
