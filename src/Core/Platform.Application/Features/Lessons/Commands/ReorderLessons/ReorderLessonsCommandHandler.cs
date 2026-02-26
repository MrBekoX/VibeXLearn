using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Lessons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Lessons.Commands.ReorderLessons;

public sealed class ReorderLessonsCommandHandler(
    IReadRepository<Lesson> readRepo,
    IWriteRepository<Lesson> writeRepo,
    ILessonBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<ReorderLessonsCommandHandler> logger) : IRequestHandler<ReorderLessonsCommand, Result>
{
    public async Task<Result> Handle(ReorderLessonsCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct, rules.CourseMustExist(request.CourseId));
        if (ruleResult.IsFailure) return ruleResult;

        if (request.Lessons.Select(x => x.LessonId).Distinct().Count() != request.Lessons.Count)
            return Result.Fail("LESSON_DUPLICATE_ID", "Lesson list contains duplicate lesson IDs.");

        if (request.Lessons.Select(x => x.NewOrder).Distinct().Count() != request.Lessons.Count)
            return Result.Fail("LESSON_DUPLICATE_ORDER", "Lesson list contains duplicate order values.");

        var lessons = await readRepo.GetListAsync(l => l.CourseId == request.CourseId, ct, tracking: true);

        foreach (var orderDto in request.Lessons)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == orderDto.LessonId);
            if (lesson is null)
                return Result.Fail("LESSON_NOT_FOUND", $"Lesson not found in course: {orderDto.LessonId}");

            lesson.UpdateOrder(orderDto.NewOrder);
        }

        await writeRepo.UpdateRangeAsync(lessons, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Lessons reordered for Course: {CourseId}", request.CourseId);
        return Result.Success();
    }
}
