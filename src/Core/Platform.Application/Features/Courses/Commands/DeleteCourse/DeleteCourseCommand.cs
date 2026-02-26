using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Courses.Commands.DeleteCourse;

/// <summary>
/// Command to soft delete a course.
/// </summary>
public sealed record DeleteCourseCommand(Guid CourseId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns =>
        [$"courses:id:{CourseId}", "courses:list:*", "courses:instructor:*"];
}
