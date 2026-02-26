using System.Linq.Expressions;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Queries.GetByUserOrder;

/// <summary>
/// Handler for GetByUserOrderQuery.
/// </summary>
public sealed class GetByUserOrderQueryHandler(
    IReadRepository<Order> readRepo,
    ILogger<GetByUserOrderQueryHandler> logger) : IRequestHandler<GetByUserOrderQuery, Result<PagedResult<GetByUserOrderQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "createdAt", "amount", "status" };

    private static readonly Dictionary<string, Expression<Func<Order, object>>> SortFieldMap = new()
    {
        ["createdAt"] = o => o.CreatedAt,
        ["amount"] = o => o.Amount,
        ["status"] = o => o.Status
    };

    public async Task<Result<PagedResult<GetByUserOrderQueryDto>>> Handle(
        GetByUserOrderQuery request, CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Query with pagination - filter by user
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: o => o.UserId == request.UserId,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "createdAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [o => o.Course]);

        // Map to DTOs
        var dtos = pagedEntities.Items.Select(o => new GetByUserOrderQueryDto
        {
            Id = o.Id,
            CourseId = o.CourseId,
            CourseTitle = o.Course?.Title ?? string.Empty,
            CourseThumbnailUrl = o.Course?.ThumbnailUrl,
            Amount = o.Amount,
            Currency = o.Currency,
            Status = o.Status.ToString(),
            DiscountAmount = o.DiscountAmount,
            CreatedAt = o.CreatedAt
        }).ToList();

        var result = new PagedResult<GetByUserOrderQueryDto>(
            dtos, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);

        logger.LogDebug("Orders by user retrieved: {Count} items, UserId: {UserId}",
            dtos.Count, request.UserId);

        return Result.Success(result);
    }
}
