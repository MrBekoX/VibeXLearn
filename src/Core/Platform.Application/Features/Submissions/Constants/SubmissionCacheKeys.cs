namespace Platform.Application.Features.Submissions.Constants;

/// <summary>
/// Submission cache key definitions.
/// </summary>
public static class SubmissionCacheKeys
{
    /// <summary>
    /// Cache key for single submission by ID.
    /// </summary>
    public static string GetById(Guid id) => $"submissions:id:{id}";

    /// <summary>
    /// Cache key for submissions by student.
    /// </summary>
    public static string ByStudent(Guid studentId) => $"submissions:student:{studentId}";

    /// <summary>
    /// Cache key for submissions by lesson.
    /// </summary>
    public static string ByLesson(Guid lessonId) => $"submissions:lesson:{lessonId}";

    /// <summary>
    /// Pattern for invalidating all submission cache entries.
    /// </summary>
    public static string InvalidateAll() => "submissions:*";
}
