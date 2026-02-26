using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Common.Results;
using Platform.Application.Features.Orders.DTOs;

namespace Platform.Application.Features.Orders.Queries.GetByUserOrder;

/// <summary>
/// Query to get orders by user.
/// </summary>
public sealed record GetByUserOrderQuery(
    Guid UserId,
    PageRequest PageRequest) : IRequest<Result<PagedResult<GetByUserOrderQueryDto>>>, IPagedQuery;
