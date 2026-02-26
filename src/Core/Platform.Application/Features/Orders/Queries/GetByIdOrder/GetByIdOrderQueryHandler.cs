using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Queries.GetByIdOrder;

/// <summary>
/// Handler for GetByIdOrderQuery.
/// </summary>
public sealed class GetByIdOrderQueryHandler(
    IReadRepository<Order> readRepo) : IRequestHandler<GetByIdOrderQuery, Result<GetByIdOrderQueryDto>>
{
    public async Task<Result<GetByIdOrderQueryDto>> Handle(
        GetByIdOrderQuery request, CancellationToken ct)
    {

        // Get order with includes
        var order = await readRepo.GetAsync(
            predicate: o => o.Id == request.OrderId,
            ct: ct,
            includes: [o => o.Course, o => o.Coupon!, o => o.PaymentIntent!]);

        if (order is null)
            return Result.Fail<GetByIdOrderQueryDto>("ORDER_NOT_FOUND", OrderBusinessMessages.NotFoundById);

        // Map to DTO
        var dto = new GetByIdOrderQueryDto
        {
            Id = order.Id,
            UserId = order.UserId,
            CourseId = order.CourseId,
            CourseTitle = order.Course?.Title ?? string.Empty,
            CourseThumbnailUrl = order.Course?.ThumbnailUrl,
            Amount = order.Amount,
            Currency = order.Currency,
            Status = order.Status.ToString(),
            DiscountAmount = order.DiscountAmount,
            CouponCode = order.Coupon?.Code,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            PaymentIntent = order.PaymentIntent is not null
                ? new PaymentIntentSummaryDto
                {
                    Id = order.PaymentIntent.Id,
                    Status = order.PaymentIntent.Status.ToString(),
                    FailReason = order.PaymentIntent.FailReason
                }
                : null
        };

        return Result.Success(dto);
    }
}
