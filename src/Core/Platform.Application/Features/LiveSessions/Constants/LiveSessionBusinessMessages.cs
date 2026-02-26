namespace Platform.Application.Features.LiveSessions.Constants;

/// <summary>
/// Live session business rule messages.
/// </summary>
public static class LiveSessionBusinessMessages
{
    // Not Found
    public const string NotFound = "Live session not found.";
    public const string NotFoundById = "Live session not found with the specified ID.";
    public const string LessonNotFound = "Lesson not found.";

    // Status Errors
    public const string AlreadyStarted = "Live session has already started.";
    public const string AlreadyEnded = "Live session has already ended.";
    public const string NotScheduled = "Only scheduled sessions can be modified.";
    public const string CannotStartEnded = "Cannot start a session that has already ended.";
    public const string CannotEndNotStarted = "Cannot end a session that has not started.";

    // Scheduling Errors
    public const string StartTimeInPast = "Start time cannot be in the past.";
    public const string DurationTooShort = "Duration must be at least 15 minutes.";
    public const string DurationTooLong = "Duration cannot exceed 480 minutes (8 hours).";
    public const string LessonAlreadyHasSession = "This lesson already has a live session scheduled.";
    public const string ConflictWithExistingSession = "This time slot conflicts with an existing session.";

    // Success Messages
    public const string ScheduledSuccessfully = "Live session scheduled successfully.";
    public const string StartedSuccessfully = "Live session started successfully.";
    public const string EndedSuccessfully = "Live session ended successfully.";
    public const string CancelledSuccessfully = "Live session cancelled successfully.";
}
