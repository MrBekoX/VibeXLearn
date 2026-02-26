namespace Platform.Application.Features.Orders.DTOs;

/// <summary>
/// DTO for order list items (all orders).
/// </summary>
public sealed record GetAllOrdersQueryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = default!;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? CourseThumbnailUrl { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public string Status { get; init; } = default!;
    public decimal? DiscountAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for order details.
/// </summary>
public sealed record GetByIdOrderQueryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? CourseThumbnailUrl { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public string Status { get; init; } = default!;
    public decimal? DiscountAmount { get; init; }
    public string? CouponCode { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public PaymentIntentSummaryDto? PaymentIntent { get; init; }
}

/// <summary>
/// DTO for payment intent summary in order.
/// </summary>
public sealed record PaymentIntentSummaryDto
{
    public Guid Id { get; init; }
    public string Status { get; init; } = default!;
    public string? FailReason { get; init; }
}

/// <summary>
/// DTO for order list items.
/// </summary>
public sealed record GetByUserOrderQueryDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? CourseThumbnailUrl { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public string Status { get; init; } = default!;
    public decimal? DiscountAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for order creation request.
/// </summary>
public sealed record CreateOrderCommandDto
{
    public Guid CourseId { get; init; }
    public string? CouponCode { get; init; }
}

/// <summary>
/// DTO for coupon application request.
/// </summary>
public sealed record ApplyCouponCommandDto
{
    public Guid OrderId { get; init; }
    public string CouponCode { get; init; } = default!;
}
