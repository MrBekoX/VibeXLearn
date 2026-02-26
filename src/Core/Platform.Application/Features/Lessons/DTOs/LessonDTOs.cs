namespace Platform.Application.Features.Lessons.DTOs;

public sealed record GetByIdLessonQueryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string? Description { get; init; }
    public string? VideoUrl { get; init; }
    public int Order { get; init; }
    public string Type { get; init; } = default!;
    public bool IsFree { get; init; }
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}

public sealed record GetByCourseLessonQueryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public int Order { get; init; }
    public string Type { get; init; } = default!;
    public bool IsFree { get; init; }
    public string? VideoUrl { get; init; }
}

public sealed record CreateLessonCommandDto
{
    public Guid CourseId { get; init; }
    public string Title { get; init; } = default!;
    public string? Description { get; init; }
    public string? VideoUrl { get; init; }
    public int Order { get; init; }
    public string Type { get; init; } = default!;
    public bool IsFree { get; init; }
}

public sealed record LessonOrderDto
{
    public Guid LessonId { get; init; }
    public int NewOrder { get; init; }
}
