using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.DTOs;

namespace Platform.Application.Features.Courses.Queries.GetByInstructorCourse;

/// <summary>
/// Query to get courses by instructor.
/// </summary>
public sealed record GetByInstructorCourseQuery(
    Guid InstructorId,
    PageRequest PageRequest) : IRequest<Result<PagedResult<GetByInstructorCourseQueryDto>>>, IPagedQuery, ICacheableQuery
{
    public string CacheKey => CourseCacheKeys.ByInstructor(InstructorId, PageRequest);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
