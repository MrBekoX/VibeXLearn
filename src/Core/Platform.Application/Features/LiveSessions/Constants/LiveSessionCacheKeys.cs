namespace Platform.Application.Features.LiveSessions.Constants;

/// <summary>
/// Live session cache key definitions.
/// </summary>
public static class LiveSessionCacheKeys
{
    /// <summary>
    /// Cache key for single live session by ID.
    /// </summary>
    public static string GetById(Guid id) => $"livesessions:id:{id}";

    /// <summary>
    /// Cache key for live session by lesson.
    /// </summary>
    public static string ByLesson(Guid lessonId) => $"livesessions:lesson:{lessonId}";

    /// <summary>
    /// Cache key for upcoming live sessions.
    /// </summary>
    public static string Upcoming() => "livesessions:upcoming";

    /// <summary>
    /// Pattern for invalidating all live session cache entries.
    /// </summary>
    public static string InvalidateAll() => "livesessions:*";
}
