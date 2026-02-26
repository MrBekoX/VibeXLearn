using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Lessons.Constants;
using Platform.Application.Features.Lessons.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Lessons.Commands.CreateLesson;

public sealed class CreateLessonCommandHandler(
    IWriteRepository<Lesson> writeRepo,
    ILessonBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CreateLessonCommandHandler> logger) : IRequestHandler<CreateLessonCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLessonCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CourseMustExist(request.CourseId),
            rules.LessonOrderMustBeUnique(request.CourseId, request.Order));

        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        var lesson = Lesson.Create(
            request.CourseId, request.Title, request.Order, request.Type,
            request.Description, request.IsFree);

        if (!string.IsNullOrWhiteSpace(request.VideoUrl) && request.Type == Platform.Domain.Enums.LessonType.Video)
            lesson.UpdateVideoUrl(request.VideoUrl);

        await writeRepo.AddAsync(lesson, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Lesson created: {LessonId} in Course: {CourseId}", lesson.Id, request.CourseId);
        return Result.Success(lesson.Id);
    }
}
