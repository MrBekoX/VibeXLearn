namespace Platform.Application.Features.Submissions.DTOs;

/// <summary>
/// DTO for submission details.
/// </summary>
public sealed record GetByIdSubmissionQueryDto
{
    public Guid Id { get; init; }
    public string RepoUrl { get; init; } = default!;
    public string? CommitSha { get; init; }
    public string? Branch { get; init; }
    public string? PrUrl { get; init; }
    public string Status { get; init; } = default!;
    public string? ReviewNote { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = default!;
    public Guid LessonId { get; init; }
    public string LessonTitle { get; init; } = default!;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for submission list items.
/// </summary>
public sealed record GetAllSubmissionsQueryDto
{
    public Guid Id { get; init; }
    public string RepoUrl { get; init; } = default!;
    public string Status { get; init; } = default!;
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = default!;
    public Guid LessonId { get; init; }
    public string LessonTitle { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for submission creation request.
/// </summary>
public sealed record CreateSubmissionCommandDto
{
    public Guid StudentId { get; init; }
    public Guid LessonId { get; init; }
    public string RepoUrl { get; init; } = default!;
    public string? CommitSha { get; init; }
    public string? Branch { get; init; }
}

/// <summary>
/// DTO for submission review request.
/// </summary>
public sealed record ReviewSubmissionCommandDto
{
    public Guid SubmissionId { get; init; }
    public bool Accept { get; init; }
    public string? ReviewNote { get; init; }
}

/// <summary>
/// DTO for student's submission list.
/// </summary>
public sealed record GetByStudentSubmissionQueryDto
{
    public Guid Id { get; init; }
    public Guid LessonId { get; init; }
    public string LessonTitle { get; init; } = default!;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string RepoUrl { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}
