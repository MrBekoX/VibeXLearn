namespace Platform.Application.Features.LiveSessions.Constants;

/// <summary>
/// Live session validation messages.
/// </summary>
public static class LiveSessionValidationMessages
{
    // ID
    public const string LiveSessionIdRequired = "Live session ID is required.";
    public const string LiveSessionIdEmpty = "Live session ID cannot be empty.";

    // Lesson ID
    public const string LessonIdRequired = "Lesson ID is required.";
    public const string LessonIdEmpty = "Lesson ID cannot be empty.";

    // Topic
    public const string TopicRequired = "Topic is required.";
    public const string TopicMaxLength = "Topic cannot exceed 300 characters.";
    public const string TopicMinLength = "Topic must be at least 3 characters.";

    // Duration
    public const string DurationRequired = "Duration is required.";
    public const string DurationMin = "Duration must be at least 15 minutes.";
    public const string DurationMax = "Duration cannot exceed 480 minutes (8 hours).";

    // Start Time
    public const string StartTimeRequired = "Start time is required.";
    public const string StartTimeInPast = "Start time cannot be in the past.";
}
