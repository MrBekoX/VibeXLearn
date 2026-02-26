using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.DTOs;

namespace Platform.Application.Features.Courses.Queries.GetBySlugCourse;

/// <summary>
/// Query to get course by slug.
/// </summary>
public sealed record GetBySlugCourseQuery(string Slug)
    : IRequest<Result<GetBySlugCourseQueryDto>>, ICacheableQuery
{
    public string CacheKey => CourseCacheKeys.BySlug(Slug);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
