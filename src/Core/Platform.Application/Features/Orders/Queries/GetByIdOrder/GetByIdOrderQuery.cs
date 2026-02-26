using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Orders.DTOs;

namespace Platform.Application.Features.Orders.Queries.GetByIdOrder;

/// <summary>
/// Query to get order by ID.
/// </summary>
public sealed record GetByIdOrderQuery(Guid OrderId) : IRequest<Result<GetByIdOrderQueryDto>>;
