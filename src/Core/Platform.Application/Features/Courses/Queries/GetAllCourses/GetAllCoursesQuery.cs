using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.DTOs;

namespace Platform.Application.Features.Courses.Queries.GetAllCourses;

/// <summary>
/// Query to get paginated list of courses.
/// </summary>
public sealed record GetAllCoursesQuery(PageRequest PageRequest)
    : IRequest<Result<PagedResult<GetAllCoursesQueryDto>>>, IPagedQuery, ICacheableQuery
{
    public string CacheKey => CourseCacheKeys.GetAll(PageRequest.Normalize());
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
