using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Lessons.DTOs;

namespace Platform.Application.Features.Lessons.Commands.ReorderLessons;

public sealed record ReorderLessonsCommand(
    Guid CourseId,
    IList<LessonOrderDto> Lessons) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["lessons:*"];
}
