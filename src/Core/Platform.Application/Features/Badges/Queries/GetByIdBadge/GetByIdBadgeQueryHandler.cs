using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Badges.Constants;
using Platform.Application.Features.Badges.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Badges.Queries.GetByIdBadge;

/// <summary>
/// Handler for GetByIdBadgeQuery.
/// Cache is managed automatically by QueryCachingBehavior — no manual cache calls needed.
/// </summary>
public sealed class GetByIdBadgeQueryHandler(
    IReadRepository<Badge> repo) : IRequestHandler<GetByIdBadgeQuery, Result<GetByIdBadgeQueryDto>>
{
    public async Task<Result<GetByIdBadgeQueryDto>> Handle(GetByIdBadgeQuery request, CancellationToken ct)
    {
        var badge = await repo.GetByIdAsync(request.BadgeId, ct);
        if (badge is null)
            return Result.Fail<GetByIdBadgeQueryDto>("BADGE_NOT_FOUND", BadgeBusinessMessages.NotFoundById);

        var dto = new GetByIdBadgeQueryDto
        {
            Id = badge.Id,
            Name = badge.Name,
            Description = badge.Description,
            IconUrl = badge.IconUrl,
            Criteria = badge.Criteria,
            CreatedAt = badge.CreatedAt,
            UpdatedAt = badge.UpdatedAt,
            UserCount = badge.UserBadges?.Count ?? 0
        };

        return Result.Success(dto);
    }
}
