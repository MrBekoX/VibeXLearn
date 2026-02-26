namespace Platform.Application.Features.Payments.DTOs;

/// <summary>
/// DTO for checkout initiation response.
/// </summary>
public sealed record CheckoutResponseDto
{
    /// <summary>
    /// Iyzico checkout form content (HTML/JS to embed).
    /// </summary>
    public string CheckoutFormContent { get; init; } = default!;

    /// <summary>
    /// Associated order ID.
    /// </summary>
    public Guid OrderId { get; init; }

    /// <summary>
    /// Payment conversation ID for tracking.
    /// </summary>
    public string ConversationId { get; init; } = default!;
}

/// <summary>
/// DTO for payment list items (all payments).
/// </summary>
public sealed record GetAllPaymentsQueryDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string ConversationId { get; init; } = default!;
    public string? IyzicoPaymentId { get; init; }
    public decimal ExpectedPrice { get; init; }
    public string Currency { get; init; } = "TRY";
    public string Status { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for payment intent details.
/// </summary>
public sealed record GetByIdPaymentQueryDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string ConversationId { get; init; } = default!;
    public string? IyzicoToken { get; init; }
    public string? IyzicoPaymentId { get; init; }
    public decimal ExpectedPrice { get; init; }
    public string Currency { get; init; } = "TRY";
    public string Status { get; init; } = default!;
    public string? FailReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for payment by order.
/// </summary>
public sealed record GetByOrderPaymentQueryDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string ConversationId { get; init; } = default!;
    public string Status { get; init; } = default!;
    public decimal ExpectedPrice { get; init; }
    public string? FailReason { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for checkout initiation request.
/// </summary>
public sealed record InitiateCheckoutCommandDto
{
    public Guid CourseId { get; init; }
    public string? CouponCode { get; init; }
}

/// <summary>
/// DTO for callback processing request.
/// </summary>
public sealed record ProcessCallbackCommandDto
{
    public string Token { get; init; } = default!;
    public string ConversationId { get; init; } = default!;
}
