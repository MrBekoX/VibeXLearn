using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Lessons.Constants;
using Platform.Application.Features.Lessons.DTOs;

namespace Platform.Application.Features.Lessons.Queries.GetByIdLesson;

public sealed record GetByIdLessonQuery(Guid LessonId)
    : IRequest<Result<GetByIdLessonQueryDto>>, ICacheableQuery
{
    public string CacheKey => LessonCacheKeys.GetById(LessonId);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
