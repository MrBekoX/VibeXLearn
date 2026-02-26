using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Application.Features.LiveSessions.DTOs;

namespace Platform.Application.Features.LiveSessions.Queries.GetByIdLiveSession;

/// <summary>
/// Query to get a live session by ID.
/// </summary>
public sealed record GetByIdLiveSessionQuery(Guid LiveSessionId)
    : IRequest<Result<GetByIdLiveSessionQueryDto>>, ICacheableQuery
{
    public string CacheKey => LiveSessionCacheKeys.GetById(LiveSessionId);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
