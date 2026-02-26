using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Domain.Entities;

namespace Platform.Application.Features.LiveSessions.Commands.EndLiveSession;

/// <summary>
/// Handler for EndLiveSessionCommand.
/// </summary>
public sealed class EndLiveSessionCommandHandler(
    IReadRepository<LiveSession> readRepo,
    IWriteRepository<LiveSession> writeRepo,
    IUnitOfWork uow,
    ILogger<EndLiveSessionCommandHandler> logger) : IRequestHandler<EndLiveSessionCommand, Result>
{
    public async Task<Result> Handle(EndLiveSessionCommand request, CancellationToken ct)
    {
        var liveSession = await readRepo.GetByIdAsync(request.LiveSessionId, ct, tracking: true);
        if (liveSession is null)
            return Result.Fail("LIVESESSION_NOT_FOUND", LiveSessionBusinessMessages.NotFound);

        liveSession.End();
        await writeRepo.UpdateAsync(liveSession, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Live session ended: {LiveSessionId}", request.LiveSessionId);

        return Result.Success();
    }
}
