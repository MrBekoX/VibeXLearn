using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.LiveSessions.Rules;

/// <summary>
/// Live session business rules implementation.
/// </summary>
public sealed class LiveSessionBusinessRules(IReadRepository<LiveSession> repo) : ILiveSessionBusinessRules
{
    public IBusinessRule LiveSessionMustExist(Guid liveSessionId)
        => new BusinessRule(
            "LIVESESSION_NOT_FOUND",
            LiveSessionBusinessMessages.NotFound,
            async ct => await repo.AnyAsync(ls => ls.Id == liveSessionId, ct)
                ? Result.Success()
                : Result.Fail(LiveSessionBusinessMessages.NotFound));

    public IBusinessRule LiveSessionMustBeScheduled(Guid liveSessionId)
        => new BusinessRule(
            "LIVESESSION_NOT_SCHEDULED",
            LiveSessionBusinessMessages.NotScheduled,
            async ct =>
            {
                var liveSession = await repo.GetByIdAsync(liveSessionId, ct);
                return liveSession?.Status == LiveSessionStatus.Scheduled
                    ? Result.Success()
                    : Result.Fail(LiveSessionBusinessMessages.NotScheduled);
            });

    public IBusinessRule LiveSessionMustNotBeEnded(Guid liveSessionId)
        => new BusinessRule(
            "LIVESESSION_ENDED",
            LiveSessionBusinessMessages.AlreadyEnded,
            async ct =>
            {
                var liveSession = await repo.GetByIdAsync(liveSessionId, ct);
                return liveSession?.Status != LiveSessionStatus.Ended
                    ? Result.Success()
                    : Result.Fail(LiveSessionBusinessMessages.AlreadyEnded);
            });

    public IBusinessRule LessonMustNotHaveLiveSession(Guid lessonId)
        => new BusinessRule(
            "LIVESESSION_LESSON_EXISTS",
            LiveSessionBusinessMessages.LessonAlreadyHasSession,
            async ct =>
            {
                var exists = await repo.AnyAsync(ls => ls.LessonId == lessonId && ls.Status != LiveSessionStatus.Ended, ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail(LiveSessionBusinessMessages.LessonAlreadyHasSession);
            });

    public IBusinessRule StartTimeMustBeInFuture(DateTime startTime)
        => new BusinessRule(
            "LIVESESSION_START_PAST",
            LiveSessionBusinessMessages.StartTimeInPast,
            ct =>
            {
                return Task.FromResult(startTime > DateTime.UtcNow
                    ? Result.Success()
                    : Result.Fail(LiveSessionBusinessMessages.StartTimeInPast));
            });
}

/// <summary>
/// Simple business rule implementation.
/// </summary>
file sealed class BusinessRule(string code, string message, Func<CancellationToken, Task<Result>> checkFunc)
    : IBusinessRule
{
    public string Code => code;
    public string Message => message;
    public Task<Result> CheckAsync(CancellationToken ct) => checkFunc(ct);
}
