using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Orders.DTOs;

namespace Platform.Application.Features.Orders.Queries.GetAllOrders;

/// <summary>
/// Query to get all orders with pagination.
/// </summary>
public sealed record GetAllOrdersQuery(PageRequest PageRequest)
    : IRequest<Result<PagedResult<GetAllOrdersQueryDto>>>, IPagedQuery;
