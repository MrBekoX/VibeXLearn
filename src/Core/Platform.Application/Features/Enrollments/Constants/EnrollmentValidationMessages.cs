namespace Platform.Application.Features.Enrollments.Constants;

/// <summary>
/// Validation messages for Enrollment feature.
/// </summary>
public static class EnrollmentValidationMessages
{
    public const string EnrollmentIdRequired = "Enrollment ID is required.";
    public const string UserIdRequired = "User ID is required.";
    public const string CourseIdRequired = "Course ID is required.";
    public const string ProgressRange = "Progress must be between 0 and 100.";
}
