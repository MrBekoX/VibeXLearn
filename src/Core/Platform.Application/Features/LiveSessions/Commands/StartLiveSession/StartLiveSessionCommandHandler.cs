using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Application.Features.LiveSessions.DTOs;
using Platform.Application.Features.LiveSessions.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.LiveSessions.Commands.StartLiveSession;

/// <summary>
/// Handler for StartLiveSessionCommand.
/// </summary>
public sealed class StartLiveSessionCommandHandler(
    IReadRepository<LiveSession> readRepo,
    IWriteRepository<LiveSession> writeRepo,
    ILiveSessionBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<StartLiveSessionCommandHandler> logger) : IRequestHandler<StartLiveSessionCommand, Result<StartLiveSessionResponseDto>>
{
    public async Task<Result<StartLiveSessionResponseDto>> Handle(StartLiveSessionCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.LiveSessionMustExist(request.LiveSessionId),
            rules.LiveSessionMustNotBeEnded(request.LiveSessionId));
        if (ruleResult.IsFailure)
            return Result.Fail<StartLiveSessionResponseDto>(ruleResult.Error);

        var liveSession = await readRepo.GetByIdAsync(request.LiveSessionId, ct, tracking: true);
        if (liveSession is null)
            return Result.Fail<StartLiveSessionResponseDto>("LIVESESSION_NOT_FOUND", LiveSessionBusinessMessages.NotFound);

        // TODO: Replace placeholder URLs with real provider response when integration is ready.
        if (string.IsNullOrWhiteSpace(liveSession.MeetingId))
        {
            liveSession.SetMeetingDetails(
                "meeting-id-placeholder",
                "https://join-url-placeholder",
                "https://host-url-placeholder");
        }

        liveSession.Start();

        await writeRepo.UpdateAsync(liveSession, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Live session started: {LiveSessionId}", request.LiveSessionId);

        return Result.Success(new StartLiveSessionResponseDto
        {
            LiveSessionId = liveSession.Id,
            MeetingId = liveSession.MeetingId,
            HostUrl = liveSession.StartUrl,
            JoinUrl = liveSession.JoinUrl
        });
    }
}
