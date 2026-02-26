using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.DTOs;

namespace Platform.Application.Features.Enrollments.Queries.GetByUserEnrollment;

/// <summary>
/// Query to get enrollments by user.
/// </summary>
public sealed record GetByUserEnrollmentQuery(
    Guid UserId,
    PageRequest PageRequest) : IRequest<Result<PagedResult<GetByUserEnrollmentQueryDto>>>, IPagedQuery, ICacheableQuery
{
    public string CacheKey
    {
        get
        {
            var pr = PageRequest.Normalize();
            return EnrollmentCacheKeys.ByUser(UserId, pr.Page, pr.PageSize);
        }
    }
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
