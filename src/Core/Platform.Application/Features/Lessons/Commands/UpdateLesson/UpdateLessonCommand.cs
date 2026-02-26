using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Lessons.Commands.UpdateLesson;

public sealed record UpdateLessonCommand(
    Guid LessonId,
    string? Title = null,
    string? Description = null,
    string? VideoUrl = null,
    int? Order = null,
    LessonType? Type = null,
    bool? IsFree = null) : IRequest<Result>, IResolvableCacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => [$"lessons:id:{LessonId}"];

    public async Task<IReadOnlyList<string>> ResolvePatternsAsync(
        IServiceProvider serviceProvider, CancellationToken ct)
    {
        var lessonRepo = serviceProvider.GetRequiredService<IReadRepository<Lesson>>();
        var lesson = await lessonRepo.GetByIdAsync(LessonId, ct);
        if (lesson is null)
            return [$"lessons:id:{LessonId}"];

        return [$"lessons:id:{LessonId}", $"lessons:course:{lesson.CourseId}"];
    }
}
