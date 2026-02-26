using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Payments.DTOs;

namespace Platform.Application.Features.Payments.Queries.GetAllPayments;

/// <summary>
/// Query to get all payments with pagination.
/// </summary>
public sealed record GetAllPaymentsQuery(PageRequest PageRequest)
    : IRequest<Result<PagedResult<GetAllPaymentsQueryDto>>>, IPagedQuery;
