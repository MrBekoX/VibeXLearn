using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.DTOs;

namespace Platform.Application.Features.Enrollments.Queries.GetAllEnrollments;

/// <summary>
/// Query to get all enrollments with pagination.
/// </summary>
public sealed record GetAllEnrollmentsQuery(PageRequest PageRequest)
    : IRequest<Result<PagedResult<GetAllEnrollmentsQueryDto>>>, IPagedQuery;
