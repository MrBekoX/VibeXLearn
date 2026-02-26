namespace Platform.Application.Features.Enrollments.DTOs;

/// <summary>
/// DTO for enrollment list items (all enrollments).
/// </summary>
public sealed record GetAllEnrollmentsQueryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = default!;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? CourseThumbnailUrl { get; init; }
    public string Status { get; init; } = default!;
    public decimal Progress { get; init; }
    public DateTime EnrolledAt { get; init; }
}

/// <summary>
/// DTO for enrollment details.
/// </summary>
public sealed record GetByIdEnrollmentQueryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? CourseThumbnailUrl { get; init; }
    public string Status { get; init; } = default!;
    public decimal Progress { get; init; }
    public DateTime EnrolledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// DTO for enrollment list items.
/// </summary>
public sealed record GetByUserEnrollmentQueryDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? CourseThumbnailUrl { get; init; }
    public string Status { get; init; } = default!;
    public decimal Progress { get; init; }
    public DateTime EnrolledAt { get; init; }
}

/// <summary>
/// DTO for course enrollments.
/// </summary>
public sealed record GetByCourseEnrollmentQueryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public decimal Progress { get; init; }
    public DateTime EnrolledAt { get; init; }
}

/// <summary>
/// DTO for progress update request.
/// </summary>
public sealed record UpdateProgressCommandDto
{
    public Guid EnrollmentId { get; init; }
    public decimal Progress { get; init; }
}
