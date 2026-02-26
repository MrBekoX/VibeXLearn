using Platform.Application.Common.Rules;

namespace Platform.Application.Features.LiveSessions.Rules;

/// <summary>
/// Live session business rules interface.
/// </summary>
public interface ILiveSessionBusinessRules
{
    /// <summary>
    /// Rule: Live session must exist in the system.
    /// </summary>
    IBusinessRule LiveSessionMustExist(Guid liveSessionId);

    /// <summary>
    /// Rule: Live session must be in Scheduled status.
    /// </summary>
    IBusinessRule LiveSessionMustBeScheduled(Guid liveSessionId);

    /// <summary>
    /// Rule: Live session must not be ended.
    /// </summary>
    IBusinessRule LiveSessionMustNotBeEnded(Guid liveSessionId);

    /// <summary>
    /// Rule: Lesson must not already have a live session.
    /// </summary>
    IBusinessRule LessonMustNotHaveLiveSession(Guid lessonId);

    /// <summary>
    /// Rule: Start time must be in the future.
    /// </summary>
    IBusinessRule StartTimeMustBeInFuture(DateTime startTime);
}
