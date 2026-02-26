using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Courses.Commands.CreateCourse;

/// <summary>
/// Command to create a new course.
/// </summary>
public sealed record CreateCourseCommand(
    string Title,
    string Slug,
    decimal Price,
    CourseLevel Level,
    Guid InstructorId,
    Guid CategoryId,
    string? Description = null,
    string? ThumbnailUrl = null) : IRequest<Result<Guid>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["courses:list:*"];
}
