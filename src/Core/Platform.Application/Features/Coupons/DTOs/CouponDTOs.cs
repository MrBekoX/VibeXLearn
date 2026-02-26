namespace Platform.Application.Features.Coupons.DTOs;

/// <summary>
/// DTO for coupon list items.
/// </summary>
public sealed record GetAllCouponsQueryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public decimal DiscountAmount { get; init; }
    public bool IsPercentage { get; init; }
    public int UsageLimit { get; init; }
    public int UsedCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for coupon details.
/// </summary>
public sealed record GetByIdCouponQueryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public decimal DiscountAmount { get; init; }
    public bool IsPercentage { get; init; }
    public int UsageLimit { get; init; }
    public int UsedCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for coupon validation.
/// </summary>
public sealed record ValidateCouponQueryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public decimal DiscountAmount { get; init; }
    public bool IsPercentage { get; init; }
    public bool IsValid { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// DTO for coupon creation request.
/// </summary>
public sealed record CreateCouponCommandDto
{
    public string Code { get; init; } = default!;
    public decimal DiscountAmount { get; init; }
    public bool IsPercentage { get; init; }
    public int UsageLimit { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// DTO for coupon update request.
/// </summary>
public sealed record UpdateCouponCommandDto
{
    public Guid CouponId { get; init; }
    public string? Code { get; init; }
    public decimal? DiscountAmount { get; init; }
    public bool? IsPercentage { get; init; }
    public int? UsageLimit { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
