using System.Linq.Expressions;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Queries.GetByCourseEnrollment;

/// <summary>
/// Handler for GetByCourseEnrollmentQuery.
/// </summary>
public sealed class GetByCourseEnrollmentQueryHandler(
    IReadRepository<Enrollment> readRepo,
    ILogger<GetByCourseEnrollmentQueryHandler> logger) : IRequestHandler<GetByCourseEnrollmentQuery, Result<PagedResult<GetByCourseEnrollmentQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "enrolledAt", "progress", "status", "userName" };

    private static readonly Dictionary<string, Expression<Func<Enrollment, object>>> SortFieldMap = new()
    {
        ["enrolledAt"] = e => e.CreatedAt,
        ["progress"] = e => e.Progress,
        ["status"] = e => e.Status
    };

    public async Task<Result<PagedResult<GetByCourseEnrollmentQueryDto>>> Handle(
        GetByCourseEnrollmentQuery request, CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Query with pagination
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: e => e.CourseId == request.CourseId,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "enrolledAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [e => e.User]);

        // Map to DTOs
        var dtos = pagedEntities.Items.Select(e => new GetByCourseEnrollmentQueryDto
        {
            Id = e.Id,
            UserId = e.UserId,
            UserName = e.User != null ? $"{e.User.FirstName} {e.User.LastName}" : string.Empty,
            Status = e.Status.ToString(),
            Progress = e.Progress,
            EnrolledAt = e.CreatedAt
        }).ToList();

        var result = new PagedResult<GetByCourseEnrollmentQueryDto>(
            dtos, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);

        logger.LogDebug("Enrollments by course retrieved: {Count} items, CourseId: {CourseId}",
            dtos.Count, request.CourseId);

        return Result.Success(result);
    }
}
