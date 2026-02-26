using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Lessons.Constants;
using Platform.Application.Features.Lessons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Lessons.Commands.UpdateLesson;

public sealed class UpdateLessonCommandHandler(
    IReadRepository<Lesson> readRepo,
    IWriteRepository<Lesson> writeRepo,
    ILessonBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<UpdateLessonCommandHandler> logger) : IRequestHandler<UpdateLessonCommand, Result>
{
    public async Task<Result> Handle(UpdateLessonCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct, rules.LessonMustExist(request.LessonId));
        if (ruleResult.IsFailure) return ruleResult;

        if (request.Order.HasValue)
        {
            var lesson = await readRepo.GetByIdAsync(request.LessonId, ct);
            if (lesson is not null)
            {
                var orderResult = await ruleEngine.RunAsync(ct,
                    rules.LessonOrderMustBeUnique(lesson.CourseId, request.Order.Value, request.LessonId));
                if (orderResult.IsFailure) return orderResult;
            }
        }

        var entity = await readRepo.GetByIdAsync(request.LessonId, ct, tracking: true);
        if (entity is null)
            return Result.Fail("LESSON_NOT_FOUND", LessonBusinessMessages.NotFoundById);

        if (request.Type.HasValue)
            entity.ChangeType(request.Type.Value);

        if (request.Title is not null)
            entity.UpdateTitle(request.Title);

        if (request.Description is not null)
            entity.UpdateDescription(request.Description);

        if (request.Order.HasValue)
            entity.UpdateOrder(request.Order.Value);

        if (request.VideoUrl is not null)
            entity.UpdateVideoUrl(request.VideoUrl);

        if (request.IsFree.HasValue)
        {
            if (request.IsFree.Value)
                entity.MarkAsFree();
            else
                entity.MarkAsPaid();
        }

        await writeRepo.UpdateAsync(entity, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Lesson updated: {LessonId}", request.LessonId);
        return Result.Success();
    }
}
