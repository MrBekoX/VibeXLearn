using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Courses.Commands.ArchiveCourse;

/// <summary>
/// Command to archive a published course.
/// </summary>
public sealed record ArchiveCourseCommand(Guid CourseId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns =>
        [$"courses:id:{CourseId}", "courses:list:*"];
}
