using System.Linq.Expressions;
using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Queries.GetByUserEnrollment;

/// <summary>
/// Handler for GetByUserEnrollmentQuery.
/// </summary>
public sealed class GetByUserEnrollmentQueryHandler(
    IReadRepository<Enrollment> readRepo,
    ILogger<GetByUserEnrollmentQueryHandler> logger) : IRequestHandler<GetByUserEnrollmentQuery, Result<PagedResult<GetByUserEnrollmentQueryDto>>>
{
    private static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "enrolledAt", "progress", "status" };

    private static readonly Dictionary<string, Expression<Func<Enrollment, object>>> SortFieldMap = new()
    {
        ["enrolledAt"] = e => e.CreatedAt,
        ["progress"] = e => e.Progress,
        ["status"] = e => e.Status
    };

    public async Task<Result<PagedResult<GetByUserEnrollmentQueryDto>>> Handle(
        GetByUserEnrollmentQuery request, CancellationToken ct)
    {
        var pr = request.PageRequest.Normalize();

        // Parse sort
        var sortDescriptors = SortParser.Parse(pr.Sort, AllowedSortFields);

        // Query with pagination
        var pagedEntities = await readRepo.GetPagedAsync(
            predicate: e => e.UserId == request.UserId,
            orderBy: q => SortParser.ApplySort(q, sortDescriptors, "enrolledAt", SortFieldMap),
            page: pr.Page,
            pageSize: pr.PageSize,
            ct: ct,
            includes: [e => e.Course]);

        // Map to DTOs
        var dtos = pagedEntities.Items.Select(e => new GetByUserEnrollmentQueryDto
        {
            Id = e.Id,
            CourseId = e.CourseId,
            CourseTitle = e.Course?.Title ?? string.Empty,
            CourseThumbnailUrl = e.Course?.ThumbnailUrl,
            Status = e.Status.ToString(),
            Progress = e.Progress,
            EnrolledAt = e.CreatedAt
        }).ToList();

        var result = new PagedResult<GetByUserEnrollmentQueryDto>(
            dtos, pagedEntities.TotalCount, pagedEntities.Page, pagedEntities.PageSize);

        logger.LogDebug("Enrollments by user retrieved: {Count} items, UserId: {UserId}",
            dtos.Count, request.UserId);

        return Result.Success(result);
    }
}
