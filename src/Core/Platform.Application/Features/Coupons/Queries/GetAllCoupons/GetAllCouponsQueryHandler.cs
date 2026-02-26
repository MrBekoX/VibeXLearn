using System.Linq.Expressions;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Queries.GetAllCoupons;

/// <summary>
/// Handler for GetAllCouponsQuery.
/// </summary>
public sealed class GetAllCouponsQueryHandler(
    IReadRepository<Coupon> readRepo,
    ILogger<GetAllCouponsQueryHandler> logger) : IRequestHandler<GetAllCouponsQuery, Result<PagedResult<GetAllCouponsQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "code", "createdAt", "expiresAt", "usedCount", "isActive" };

    private static readonly Dictionary<string, Expression<Func<Coupon, object>>> SortFieldMap = new()
    {
        ["code"] = c => c.Code,
        ["createdAt"] = c => c.CreatedAt,
        ["expiresAt"] = c => c.ExpiresAt,
        ["usedCount"] = c => c.UsedCount,
        ["isActive"] = c => c.IsActive
    };

    public async Task<Result<PagedResult<GetAllCouponsQueryDto>>> Handle(
        GetAllCouponsQuery request, CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Query with pagination
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: null,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "createdAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct);

        // Map to DTOs
        var dtos = pagedEntities.Items.Select(c => new GetAllCouponsQueryDto
        {
            Id = c.Id,
            Code = c.Code,
            DiscountAmount = c.DiscountAmount,
            IsPercentage = c.IsPercentage,
            UsageLimit = c.UsageLimit,
            UsedCount = c.UsedCount,
            IsActive = c.IsActive,
            ExpiresAt = c.ExpiresAt,
            CreatedAt = c.CreatedAt
        }).ToList();

        var result = new PagedResult<GetAllCouponsQueryDto>(
            dtos, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);

        logger.LogDebug("Coupons retrieved: {Count} items", dtos.Count);

        return Result.Success(result);
    }
}
