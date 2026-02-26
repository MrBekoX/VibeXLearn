using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Lessons.Commands.CreateLesson;

public sealed record CreateLessonCommand(
    Guid CourseId,
    string Title,
    int Order,
    LessonType Type,
    string? Description = null,
    string? VideoUrl = null,
    bool IsFree = false) : IRequest<Result<Guid>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns =>
        [$"lessons:course:{CourseId}", $"courses:id:{CourseId}"];
}
