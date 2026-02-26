using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Application.Features.LiveSessions.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.LiveSessions.Queries.GetByIdLiveSession;

/// <summary>
/// Handler for GetByIdLiveSessionQuery.
/// </summary>
public sealed class GetByIdLiveSessionQueryHandler(
    IReadRepository<LiveSession> repo) : IRequestHandler<GetByIdLiveSessionQuery, Result<GetByIdLiveSessionQueryDto>>
{
    public async Task<Result<GetByIdLiveSessionQueryDto>> Handle(
        GetByIdLiveSessionQuery request,
        CancellationToken ct)
    {

        var liveSession = await repo.GetAsync(
            ls => ls.Id == request.LiveSessionId, ct,
            includes: [ls => ls.Lesson, ls => ls.Lesson.Course]);

        if (liveSession is null)
            return Result.Fail<GetByIdLiveSessionQueryDto>("LIVESESSION_NOT_FOUND", LiveSessionBusinessMessages.NotFoundById);

        var dto = new GetByIdLiveSessionQueryDto
        {
            Id = liveSession.Id,
            Topic = liveSession.Topic,
            StartTime = liveSession.StartTime,
            DurationMin = liveSession.DurationMin,
            MeetingId = liveSession.MeetingId,
            JoinUrl = liveSession.JoinUrl,
            HostUrl = liveSession.StartUrl,
            Status = liveSession.Status.ToString(),
            LessonId = liveSession.LessonId,
            LessonTitle = liveSession.Lesson?.Title ?? string.Empty,
            CourseId = liveSession.Lesson?.CourseId ?? Guid.Empty,
            CourseTitle = liveSession.Lesson?.Course?.Title ?? string.Empty,
            CreatedAt = liveSession.CreatedAt
        };
        return Result.Success(dto);
    }
}
