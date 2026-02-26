using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.DTOs;

namespace Platform.Application.Features.Enrollments.Queries.GetByCourseEnrollment;

/// <summary>
/// Query to get enrollments by course (for instructors/admins).
/// </summary>
public sealed record GetByCourseEnrollmentQuery(
    Guid CourseId,
    PageRequest PageRequest) : IRequest<Result<PagedResult<GetByCourseEnrollmentQueryDto>>>, IPagedQuery, ICacheableQuery
{
    public string CacheKey
    {
        get
        {
            var pr = PageRequest.Normalize();
            return EnrollmentCacheKeys.ByCourse(CourseId, pr.Page, pr.PageSize);
        }
    }
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
