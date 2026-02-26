using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Badges.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Badges.Queries.GetAllBadges;

/// <summary>
/// Handler for GetAllBadgesQuery.
/// Cache is managed automatically by QueryCachingBehavior — no manual cache calls needed.
/// </summary>
public sealed class GetAllBadgesQueryHandler(
    IReadRepository<Badge> readRepo) : IRequestHandler<GetAllBadgesQuery, Result<PagedResult<GetAllBadgesQueryDto>>>
{
    public async Task<Result<PagedResult<GetAllBadgesQueryDto>>> Handle(
        GetAllBadgesQuery request,
        CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        var paged = await readRepo.GetPagedAsync(
            predicate: null,
            orderBy: q => q.OrderByDescending(b => b.CreatedAt),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct);

        var dtos = paged.Items.Select(b => new GetAllBadgesQueryDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            IconUrl = b.IconUrl,
            CreatedAt = b.CreatedAt
        }).ToList();

        var result = new PagedResult<GetAllBadgesQueryDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);

        return Result.Success(result);
    }
}
