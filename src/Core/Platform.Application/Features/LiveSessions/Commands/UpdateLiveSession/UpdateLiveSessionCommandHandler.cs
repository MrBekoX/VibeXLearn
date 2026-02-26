using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Application.Features.LiveSessions.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.LiveSessions.Commands.UpdateLiveSession;

/// <summary>
/// Handler for UpdateLiveSessionCommand.
/// </summary>
public sealed class UpdateLiveSessionCommandHandler(
    IReadRepository<LiveSession> readRepo,
    IWriteRepository<LiveSession> writeRepo,
    ILiveSessionBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<UpdateLiveSessionCommandHandler> logger) : IRequestHandler<UpdateLiveSessionCommand, Result>
{
    public async Task<Result> Handle(UpdateLiveSessionCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.LiveSessionMustExist(request.LiveSessionId),
            rules.LiveSessionMustBeScheduled(request.LiveSessionId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Validate start time if changing
        if (request.StartTime.HasValue)
        {
            var timeResult = await ruleEngine.RunAsync(ct,
                rules.StartTimeMustBeInFuture(request.StartTime.Value));
            if (timeResult.IsFailure)
                return timeResult;
        }

        // Get live session with tracking
        var liveSession = await readRepo.GetByIdAsync(request.LiveSessionId, ct, tracking: true);
        if (liveSession is null)
            return Result.Fail("LIVESESSION_NOT_FOUND", LiveSessionBusinessMessages.NotFound);

        // Update using domain method
        liveSession.Update(
            request.Topic ?? liveSession.Topic,
            request.StartTime ?? liveSession.StartTime,
            request.DurationMin ?? liveSession.DurationMin);

        await writeRepo.UpdateAsync(liveSession, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Live session updated: {LiveSessionId}", request.LiveSessionId);

        return Result.Success();
    }
}
