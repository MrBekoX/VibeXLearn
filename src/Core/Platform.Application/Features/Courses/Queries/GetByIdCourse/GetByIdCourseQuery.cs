using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.DTOs;

namespace Platform.Application.Features.Courses.Queries.GetByIdCourse;

/// <summary>
/// Query to get course by ID.
/// </summary>
public sealed record GetByIdCourseQuery(Guid CourseId)
    : IRequest<Result<GetByIdCourseQueryDto>>, ICacheableQuery
{
    public string CacheKey => CourseCacheKeys.GetById(CourseId);
    public bool BypassCache => false;
}
