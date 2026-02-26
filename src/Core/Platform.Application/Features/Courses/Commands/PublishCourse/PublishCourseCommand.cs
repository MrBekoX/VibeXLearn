using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Courses.Commands.PublishCourse;

/// <summary>
/// Command to publish a draft course.
/// </summary>
public sealed record PublishCourseCommand(Guid CourseId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns =>
        [$"courses:id:{CourseId}", "courses:list:*"];
}
