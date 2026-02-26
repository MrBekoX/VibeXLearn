using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Application.Features.LiveSessions.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.LiveSessions.Commands.CancelLiveSession;

/// <summary>
/// Handler for CancelLiveSessionCommand.
/// </summary>
public sealed class CancelLiveSessionCommandHandler(
    IReadRepository<LiveSession> readRepo,
    IWriteRepository<LiveSession> writeRepo,
    ILiveSessionBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CancelLiveSessionCommandHandler> logger) : IRequestHandler<CancelLiveSessionCommand, Result>
{
    public async Task<Result> Handle(CancelLiveSessionCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.LiveSessionMustExist(request.LiveSessionId),
            rules.LiveSessionMustNotBeEnded(request.LiveSessionId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get live session with tracking
        var liveSession = await readRepo.GetByIdAsync(request.LiveSessionId, ct, tracking: true);
        if (liveSession is null)
            return Result.Fail("LIVESESSION_NOT_FOUND", LiveSessionBusinessMessages.NotFound);

        // Cancel using domain method
        liveSession.Cancel();

        await writeRepo.UpdateAsync(liveSession, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Live session cancelled: {LiveSessionId}", request.LiveSessionId);

        return Result.Success();
    }
}
