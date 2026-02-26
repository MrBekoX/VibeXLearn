using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Lessons.Constants;
using Platform.Application.Features.Lessons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Lessons.Commands.DeleteLesson;

public sealed class DeleteLessonCommandHandler(
    IReadRepository<Lesson> readRepo,
    IWriteRepository<Lesson> writeRepo,
    ILessonBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<DeleteLessonCommandHandler> logger) : IRequestHandler<DeleteLessonCommand, Result>
{
    public async Task<Result> Handle(DeleteLessonCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct, rules.LessonMustExist(request.LessonId));
        if (ruleResult.IsFailure) return ruleResult;

        var lesson = await readRepo.GetByIdAsync(request.LessonId, ct, tracking: true);
        if (lesson is null)
            return Result.Fail("LESSON_NOT_FOUND", LessonBusinessMessages.NotFoundById);

        await writeRepo.SoftDeleteAsync(lesson, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Lesson deleted: {LessonId}", request.LessonId);
        return Result.Success();
    }
}
