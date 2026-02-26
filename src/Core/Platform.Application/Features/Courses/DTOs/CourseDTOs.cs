using Platform.Domain.Enums;

namespace Platform.Application.Features.Courses.DTOs;

/// <summary>
/// DTO for course list items.
/// </summary>
public sealed record GetAllCoursesQueryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? ThumbnailUrl { get; init; }
    public decimal Price { get; init; }
    public string Level { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int EnrollmentCount { get; init; }
    public string? CategoryName { get; init; }
    public Guid? CategoryId { get; init; }
    public string? InstructorName { get; init; }
    public Guid? InstructorId { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for detailed course information.
/// </summary>
public sealed record GetByIdCourseQueryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public decimal Price { get; init; }
    public string Level { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int EnrollmentCount { get; init; }
    public Guid CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public Guid InstructorId { get; init; }
    public string? InstructorName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IList<LessonSummaryDto> Lessons { get; init; } = [];
}

/// <summary>
/// DTO for lesson summary in course details.
/// </summary>
public sealed record LessonSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public int Order { get; init; }
    public string Type { get; init; } = default!;
    public bool IsFree { get; init; }
    public int DurationMinutes { get; init; }
}

/// <summary>
/// DTO for course creation request.
/// </summary>
public sealed record CreateCourseCommandDto
{
    public string Title { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public decimal Price { get; init; }
    public string Level { get; init; } = default!;
    public Guid InstructorId { get; init; }
    public Guid CategoryId { get; init; }
}

/// <summary>
/// DTO for course update request.
/// </summary>
public sealed record UpdateCourseCommandDto
{
    public Guid CourseId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public decimal? Price { get; init; }
    public string? Level { get; init; }
    public Guid? CategoryId { get; init; }
}

/// <summary>
/// DTO for course by slug (same as GetById).
/// </summary>
public sealed record GetBySlugCourseQueryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public decimal Price { get; init; }
    public string Level { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int EnrollmentCount { get; init; }
    public Guid CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public Guid InstructorId { get; init; }
    public string? InstructorName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IList<LessonSummaryDto> Lessons { get; init; } = [];
}

/// <summary>
/// DTO for courses by instructor.
/// </summary>
public sealed record GetByInstructorCourseQueryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? ThumbnailUrl { get; init; }
    public decimal Price { get; init; }
    public string Level { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int EnrollmentCount { get; init; }
    public string? CategoryName { get; init; }
    public Guid CategoryId { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for instructor's courses (with revenue).
/// </summary>
public sealed record GetInstructorCoursesQueryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int EnrollmentCount { get; init; }
    public decimal TotalRevenue { get; init; }
    public DateTime CreatedAt { get; init; }
}
