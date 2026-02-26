namespace Platform.Application.Features.LiveSessions.DTOs;

/// <summary>
/// DTO for live session details.
/// </summary>
public sealed record GetByIdLiveSessionQueryDto
{
    public Guid Id { get; init; }
    public string Topic { get; init; } = default!;
    public DateTime StartTime { get; init; }
    public int DurationMin { get; init; }
    public string? MeetingId { get; init; }
    public string? JoinUrl { get; init; }
    public string? HostUrl { get; init; }
    public string Status { get; init; } = default!;
    public Guid LessonId { get; init; }
    public string LessonTitle { get; init; } = default!;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for live session list items.
/// </summary>
public sealed record GetAllLiveSessionsQueryDto
{
    public Guid Id { get; init; }
    public string Topic { get; init; } = default!;
    public DateTime StartTime { get; init; }
    public int DurationMin { get; init; }
    public string Status { get; init; } = default!;
    public Guid LessonId { get; init; }
    public string LessonTitle { get; init; } = default!;
}

/// <summary>
/// DTO for schedule live session request.
/// </summary>
public sealed record ScheduleLiveSessionCommandDto
{
    public Guid LessonId { get; init; }
    public string Topic { get; init; } = default!;
    public DateTime StartTime { get; init; }
    public int DurationMin { get; init; }
}

/// <summary>
/// DTO for start live session response.
/// </summary>
public sealed record StartLiveSessionResponseDto
{
    public Guid LiveSessionId { get; init; }
    public string? MeetingId { get; init; }
    public string? HostUrl { get; init; }
    public string? JoinUrl { get; init; }
}

/// <summary>
/// DTO for upcoming live sessions.
/// </summary>
public sealed record UpcomingLiveSessionQueryDto
{
    public Guid Id { get; init; }
    public string Topic { get; init; } = default!;
    public DateTime StartTime { get; init; }
    public int DurationMin { get; init; }
    public Guid LessonId { get; init; }
    public string LessonTitle { get; init; } = default!;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? JoinUrl { get; init; }
}
