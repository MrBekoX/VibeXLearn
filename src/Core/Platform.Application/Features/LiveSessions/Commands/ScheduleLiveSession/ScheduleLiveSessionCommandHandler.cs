using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Application.Features.LiveSessions.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.LiveSessions.Commands.ScheduleLiveSession;

/// <summary>
/// Handler for ScheduleLiveSessionCommand.
/// </summary>
public sealed class ScheduleLiveSessionCommandHandler(
    IWriteRepository<LiveSession> writeRepo,
    ILiveSessionBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<ScheduleLiveSessionCommandHandler> logger) : IRequestHandler<ScheduleLiveSessionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ScheduleLiveSessionCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.LessonMustNotHaveLiveSession(request.LessonId),
            rules.StartTimeMustBeInFuture(request.StartTime));
        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        var liveSession = LiveSession.Create(request.LessonId, request.Topic, request.StartTime, request.DurationMin);
        await writeRepo.AddAsync(liveSession, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Live session scheduled: {LiveSessionId} for Lesson: {LessonId}",
            liveSession.Id, request.LessonId);

        return Result.Success(liveSession.Id);
    }
}
