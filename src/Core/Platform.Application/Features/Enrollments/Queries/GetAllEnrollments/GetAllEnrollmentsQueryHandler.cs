using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Queries.GetAllEnrollments;

/// <summary>
/// Handler for GetAllEnrollmentsQuery.
/// </summary>
public sealed class GetAllEnrollmentsQueryHandler(
    IReadRepository<Enrollment> readRepo,
    IMapper mapper) : IRequestHandler<GetAllEnrollmentsQuery, Result<PagedResult<GetAllEnrollmentsQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "progress", "status"
    };

    private static readonly Dictionary<string, Expression<Func<Enrollment, object>>> SortFieldMap = new()
    {
        ["createdAt"] = e => e.CreatedAt,
        ["progress"] = e => e.Progress,
        ["status"] = e => e.Status
    };

    public async Task<Result<PagedResult<GetAllEnrollmentsQueryDto>>> Handle(
        GetAllEnrollmentsQuery request,
        CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Build search predicate
        Expression<Func<Enrollment, bool>>? searchPredicate = null;
        if (!string.IsNullOrWhiteSpace(pr.Search))
        {
            var term = pr.Search.Trim().ToLower();
            searchPredicate = e =>
                (e.Course != null && e.Course.Title.ToLower().Contains(term)) ||
                (e.User != null && (e.User.FirstName.ToLower().Contains(term) ||
                                    e.User.LastName.ToLower().Contains(term)));
        }

        // Query with pagination
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: searchPredicate,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "createdAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [e => e.Course, e => e.User]);

        // Map to DTOs
        var result = pagedEntities.Map(e => mapper.Map<GetAllEnrollmentsQueryDto>(e));

        return Result.Success(result);
    }
}
