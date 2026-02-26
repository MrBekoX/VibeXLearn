using Platform.Domain.Common;
using Platform.Domain.Enums;
using Platform.Domain.Events;

namespace Platform.Domain.Entities;

/// <summary>
/// LiveSession with status transitions.
/// </summary>
public class LiveSession : BaseEntity, IAggregateRoot
{
    // Private setters for encapsulation
    public string           Topic       { get; private set; } = default!;
    public DateTime         StartTime   { get; private set; }
    public int              DurationMin { get; private set; }
    public string?          MeetingId   { get; private set; }
    public string?          JoinUrl     { get; private set; }
    public string?          StartUrl    { get; private set; }
    public LiveSessionStatus Status     { get; private set; } = LiveSessionStatus.Scheduled;
    public Guid             LessonId    { get; private set; }

    // Computed properties
    public DateTime EndTime => StartTime.AddMinutes(DurationMin);
    public bool     IsInProgress => Status == LiveSessionStatus.Started &&
                                     DateTime.UtcNow >= StartTime &&
                                     DateTime.UtcNow <= EndTime;
    public bool     IsUpcoming => Status == LiveSessionStatus.Scheduled &&
                                   DateTime.UtcNow < StartTime;
    public bool     HasEnded => Status == LiveSessionStatus.Ended ||
                                (Status == LiveSessionStatus.Started && DateTime.UtcNow > EndTime);

    // Navigation properties
    public Lesson           Lesson      { get; private set; } = default!;

    // Private constructor for EF Core
    private LiveSession() { }

    /// <summary>
    /// Factory method to create a new live session.
    /// </summary>
    public static LiveSession Create(
        Guid lessonId,
        string topic,
        DateTime startTime,
        int durationMin)
    {
        Guard.Against.EmptyGuid(lessonId, nameof(lessonId));
        Guard.Against.NullOrWhiteSpace(topic, nameof(topic));
        Guard.Against.NegativeOrZero(durationMin, nameof(durationMin));

        // Validate start time is in future
        if (startTime.Kind != DateTimeKind.Utc)
            startTime = startTime.ToUniversalTime();

        var session = new LiveSession
        {
            LessonId = lessonId,
            Topic = topic.Trim(),
            StartTime = startTime,
            DurationMin = durationMin,
            Status = LiveSessionStatus.Scheduled
        };

        session.AddDomainEvent(new LiveSessionScheduledEvent(session.Id, lessonId, startTime));
        return session;
    }

    /// <summary>
    /// Update session details.
    /// </summary>
    public void Update(string topic, DateTime startTime, int durationMin)
    {
        if (Status != LiveSessionStatus.Scheduled)
            throw new DomainException("LIVESESSION_UPDATE_INVALID_STATUS",
                "Only scheduled sessions can be updated.");

        Topic = Guard.Against.NullOrWhiteSpace(topic, nameof(topic)).Trim();
        StartTime = startTime.Kind == DateTimeKind.Utc ? startTime : startTime.ToUniversalTime();
        DurationMin = (int)Guard.Against.NegativeOrZero(durationMin, nameof(durationMin));
        MarkAsUpdated();
    }

    /// <summary>
    /// Set meeting details from Zoom.
    /// </summary>
    public void SetMeetingDetails(string meetingId, string joinUrl, string? startUrl = null)
    {
        if (Status != LiveSessionStatus.Scheduled)
            throw new DomainException("LIVESESSION_MEETING_INVALID_STATUS",
                "Meeting details can only be set for scheduled sessions.");

        Guard.Against.NullOrWhiteSpace(meetingId, nameof(meetingId));
        Guard.Against.NullOrWhiteSpace(joinUrl, nameof(joinUrl));

        MeetingId = meetingId;
        JoinUrl = joinUrl;
        StartUrl = startUrl;
        MarkAsUpdated();
    }

    /// <summary>
    /// Start the session.
    /// </summary>
    public void Start()
    {
        if (Status != LiveSessionStatus.Scheduled)
            throw new DomainException("LIVESESSION_START_INVALID_STATUS",
                "Only scheduled sessions can be started.");

        if (string.IsNullOrWhiteSpace(MeetingId))
            throw new DomainException("LIVESESSION_NO_MEETING",
                "Cannot start session without meeting details.");

        Status = LiveSessionStatus.Started;
        MarkAsUpdated();

        AddDomainEvent(new LiveSessionStartedEvent(Id, MeetingId!));
    }

    /// <summary>
    /// End the session.
    /// </summary>
    public void End()
    {
        if (Status != LiveSessionStatus.Started)
            throw new DomainException("LIVESESSION_END_INVALID_STATUS",
                "Only started sessions can be ended.");

        Status = LiveSessionStatus.Ended;
        MarkAsUpdated();

        AddDomainEvent(new LiveSessionEndedEvent(Id));
    }

    /// <summary>
    /// Cancel the session.
    /// </summary>
    public void Cancel()
    {
        if (Status == LiveSessionStatus.Ended)
            throw new DomainException("LIVESESSION_CANCEL_ENDED",
                "Cannot cancel an ended session.");

        Status = LiveSessionStatus.Cancelled;
        MarkAsUpdated();
    }

    /// <summary>
    /// Reschedule the session.
    /// </summary>
    public void Reschedule(DateTime newStartTime, int? newDuration = null)
    {
        if (Status == LiveSessionStatus.Ended || Status == LiveSessionStatus.Started)
            throw new DomainException("LIVESESSION_RESCHEDULE_INVALID",
                "Cannot reschedule ended or in-progress sessions.");

        StartTime = newStartTime.Kind == DateTimeKind.Utc ? newStartTime : newStartTime.ToUniversalTime();
        if (newDuration.HasValue)
            DurationMin = (int)Guard.Against.NegativeOrZero(newDuration.Value, nameof(newDuration));

        Status = LiveSessionStatus.Scheduled;
        MarkAsUpdated();

        AddDomainEvent(new LiveSessionScheduledEvent(Id, LessonId, StartTime));
    }
}
