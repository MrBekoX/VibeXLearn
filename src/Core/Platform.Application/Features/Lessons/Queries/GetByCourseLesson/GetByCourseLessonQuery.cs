using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Lessons.Constants;
using Platform.Application.Features.Lessons.DTOs;

namespace Platform.Application.Features.Lessons.Queries.GetByCourseLesson;

public sealed record GetByCourseLessonQuery(Guid CourseId)
    : IRequest<Result<IList<GetByCourseLessonQueryDto>>>, ICacheableQuery
{
    public string CacheKey => LessonCacheKeys.ByCourse(CourseId);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
