using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Courses.Commands.UpdateCourse;

/// <summary>
/// Command to update an existing course.
/// </summary>
public sealed record UpdateCourseCommand(
    Guid CourseId,
    string? Title = null,
    string? Description = null,
    string? ThumbnailUrl = null,
    decimal? Price = null,
    CourseLevel? Level = null,
    Guid? CategoryId = null) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns =>
        [$"courses:id:{CourseId}", "courses:list:*", "courses:instructor:*"];
}
