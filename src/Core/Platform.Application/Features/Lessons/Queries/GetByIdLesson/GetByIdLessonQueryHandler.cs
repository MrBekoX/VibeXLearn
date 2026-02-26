using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Lessons.Constants;
using Platform.Application.Features.Lessons.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Lessons.Queries.GetByIdLesson;

public sealed class GetByIdLessonQueryHandler(
    IReadRepository<Lesson> readRepo) : IRequestHandler<GetByIdLessonQuery, Result<GetByIdLessonQueryDto>>
{
    public async Task<Result<GetByIdLessonQueryDto>> Handle(GetByIdLessonQuery request, CancellationToken ct)
    {

        var lesson = await readRepo.GetAsync(l => l.Id == request.LessonId, ct, includes: [l => l.Course]);
        if (lesson is null)
            return Result.Fail<GetByIdLessonQueryDto>("LESSON_NOT_FOUND", LessonBusinessMessages.NotFoundById);

        var dto = new GetByIdLessonQueryDto
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            VideoUrl = lesson.VideoUrl,
            Order = lesson.Order,
            Type = lesson.Type.ToString(),
            IsFree = lesson.IsFree,
            CourseId = lesson.CourseId,
            CourseTitle = lesson.Course?.Title ?? string.Empty,
            CreatedAt = lesson.CreatedAt
        };
        return Result.Success(dto);
    }
}
