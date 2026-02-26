using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Queries.GetAllOrders;

/// <summary>
/// Handler for GetAllOrdersQuery.
/// </summary>
public sealed class GetAllOrdersQueryHandler(
    IReadRepository<Order> readRepo,
    IMapper mapper) : IRequestHandler<GetAllOrdersQuery, Result<PagedResult<GetAllOrdersQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "amount", "status"
    };

    private static readonly Dictionary<string, Expression<Func<Order, object>>> SortFieldMap = new()
    {
        ["createdAt"] = o => o.CreatedAt,
        ["amount"] = o => o.Amount,
        ["status"] = o => o.Status
    };

    public async Task<Result<PagedResult<GetAllOrdersQueryDto>>> Handle(
        GetAllOrdersQuery request,
        CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Build search predicate
        Expression<Func<Order, bool>>? searchPredicate = null;
        if (!string.IsNullOrWhiteSpace(pr.Search))
        {
            var term = pr.Search.Trim().ToLower();
            searchPredicate = o =>
                (o.Course != null && o.Course.Title.ToLower().Contains(term)) ||
                (o.User != null && o.User.Email != null && o.User.Email.ToLower().Contains(term));
        }

        // Query with pagination
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: searchPredicate,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "createdAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [o => o.Course, o => o.User]);

        // Map to DTOs
        var result = pagedEntities.Map(o => mapper.Map<GetAllOrdersQueryDto>(o));

        return Result.Success(result);
    }
}
