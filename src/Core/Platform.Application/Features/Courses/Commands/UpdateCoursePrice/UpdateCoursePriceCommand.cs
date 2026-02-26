using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Courses.Commands.UpdateCoursePrice;

/// <summary>
/// Command to update course price.
/// </summary>
public sealed record UpdateCoursePriceCommand(
    Guid CourseId,
    decimal NewPrice) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns =>
        [$"courses:id:{CourseId}", "courses:list:*"];
}
