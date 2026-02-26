using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Lessons.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Lessons.Queries.GetByCourseLesson;

/// <summary>
/// Handler for GetByCourseLessonQuery.
/// Cache is managed automatically by QueryCachingBehavior — no manual cache calls needed.
/// </summary>
public sealed class GetByCourseLessonQueryHandler(
    IReadRepository<Lesson> readRepo) : IRequestHandler<GetByCourseLessonQuery, Result<IList<GetByCourseLessonQueryDto>>>
{
    public async Task<Result<IList<GetByCourseLessonQueryDto>>> Handle(GetByCourseLessonQuery request, CancellationToken ct)
    {
        var lessons = await readRepo.GetListAsync(l => l.CourseId == request.CourseId, ct);

        var dtos = lessons.OrderBy(l => l.Order).Select(l => new GetByCourseLessonQueryDto
        {
            Id = l.Id,
            Title = l.Title,
            Order = l.Order,
            Type = l.Type.ToString(),
            IsFree = l.IsFree,
            VideoUrl = l.VideoUrl
        }).ToList();

        return Result.Success<IList<GetByCourseLessonQueryDto>>(dtos);
    }
}
