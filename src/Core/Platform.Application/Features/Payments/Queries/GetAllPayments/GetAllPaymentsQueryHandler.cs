using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Payments.Constants;
using Platform.Application.Features.Payments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Payments.Queries.GetAllPayments;

/// <summary>
/// Handler for GetAllPaymentsQuery.
/// </summary>
public sealed class GetAllPaymentsQueryHandler(
    IReadRepository<PaymentIntent> readRepo,
    IMapper mapper) : IRequestHandler<GetAllPaymentsQuery, Result<PagedResult<GetAllPaymentsQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "expectedprice", "status"
    };

    private static readonly Dictionary<string, Expression<Func<PaymentIntent, object>>> SortFieldMap = new()
    {
        ["createdAt"] = p => p.CreatedAt,
        ["expectedprice"] = p => p.ExpectedPrice,
        ["status"] = p => p.Status
    };

    public async Task<Result<PagedResult<GetAllPaymentsQueryDto>>> Handle(
        GetAllPaymentsQuery request,
        CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Build search predicate
        Expression<Func<PaymentIntent, bool>>? searchPredicate = null;
        if (!string.IsNullOrWhiteSpace(pr.Search))
        {
            var term = pr.Search.Trim().ToLower();
            searchPredicate = p =>
                p.ConversationId.ToLower().Contains(term) ||
                (p.IyzicoPaymentId != null && p.IyzicoPaymentId.ToLower().Contains(term));
        }

        // Query with pagination
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: searchPredicate,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "createdAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [p => p.Order]);

        // Map to DTOs
        var result = pagedEntities.Map(p => mapper.Map<GetAllPaymentsQueryDto>(p));

        return Result.Success(result);
    }
}
