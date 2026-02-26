using Platform.Domain.Common;

namespace Platform.Domain.Entities;

/// <summary>
/// Coupon with validation and discount calculation logic.
/// </summary>
public class Coupon : BaseEntity
{
    // Private setters for encapsulation
    public string   Code           { get; private set; } = default!;
    public decimal  DiscountAmount { get; private set; }
    public bool     IsPercentage   { get; private set; } = false;
    public int      UsageLimit     { get; private set; }
    public int      UsedCount      { get; private set; } = 0;
    public DateTime ExpiresAt      { get; private set; }
    public bool     IsActive       { get; private set; } = true;

    // Computed properties
    public bool     IsValid        => IsActive && !IsExpired && HasRemainingUses;
    public bool     IsExpired      => DateTime.UtcNow > ExpiresAt;
    public bool     HasRemainingUses => UsedCount < UsageLimit;
    public int      RemainingUses  => Math.Max(0, UsageLimit - UsedCount);

    // Navigation properties
    public ICollection<Order> Orders { get; private set; } = [];

    // Private constructor for EF Core
    private Coupon() { }

    /// <summary>
    /// Factory method to create a new coupon.
    /// </summary>
    public static Coupon Create(
        string code,
        decimal discountAmount,
        bool isPercentage,
        int usageLimit,
        DateTime expiresAt)
    {
        Guard.Against.NullOrWhiteSpace(code, nameof(code));
        Guard.Against.Negative(discountAmount, nameof(discountAmount));
        Guard.Against.NegativeOrZero(usageLimit, nameof(usageLimit));

        if (isPercentage && discountAmount > 100)
            throw new DomainException("COUPON_PERCENTAGE_INVALID",
                "Percentage discount cannot exceed 100%.");

        return new Coupon
        {
            Code = code.Trim().ToUpperInvariant(),
            DiscountAmount = discountAmount,
            IsPercentage = isPercentage,
            UsageLimit = usageLimit,
            ExpiresAt = expiresAt.Kind == DateTimeKind.Utc
                ? expiresAt
                : expiresAt.ToUniversalTime()
        };
    }

    /// <summary>
    /// Create a fixed amount coupon.
    /// </summary>
    public static Coupon CreateFixed(string code, decimal amount, int usageLimit, DateTime expiresAt)
        => Create(code, amount, false, usageLimit, expiresAt);

    /// <summary>
    /// Create a percentage coupon.
    /// </summary>
    public static Coupon CreatePercentage(string code, decimal percentage, int usageLimit, DateTime expiresAt)
        => Create(code, percentage, true, usageLimit, expiresAt);

    /// <summary>
    /// Calculate discount amount for given price.
    /// </summary>
    public decimal CalculateDiscount(decimal originalPrice)
    {
        if (!IsValid)
            throw new DomainException("COUPON_INVALID",
                "Coupon is not valid, has expired, or usage limit reached.");

        Guard.Against.Negative(originalPrice, nameof(originalPrice));

        return IsPercentage
            ? originalPrice * (DiscountAmount / 100)
            : Math.Min(DiscountAmount, originalPrice); // Cannot exceed price
    }

    /// <summary>
    /// Record usage of coupon (increment count).
    /// Called by Order aggregate when order is paid.
    /// </summary>
    public void RecordUsage()
    {
        if (!IsValid)
            throw new DomainException("COUPON_CANNOT_USE",
                "Coupon cannot be used.");

        UsedCount++;
        MarkAsUpdated();
    }

    /// <summary>
    /// Update coupon details.
    /// </summary>
    public void Update(
        string? code = null,
        decimal? discountAmount = null,
        bool? isPercentage = null,
        int? usageLimit = null,
        DateTime? expiresAt = null)
    {
        var nextCode = string.IsNullOrWhiteSpace(code) ? Code : code.Trim().ToUpperInvariant();
        var nextIsPercentage = isPercentage ?? IsPercentage;
        var nextDiscountAmount = discountAmount ?? DiscountAmount;
        var nextUsageLimit = usageLimit ?? UsageLimit;

        Guard.Against.NullOrWhiteSpace(nextCode, nameof(code));
        Guard.Against.Negative(nextDiscountAmount, nameof(discountAmount));
        Guard.Against.NegativeOrZero(nextUsageLimit, nameof(usageLimit));

        if (nextUsageLimit < UsedCount)
            throw new DomainException("COUPON_USAGE_LIMIT_INVALID",
                "Usage limit cannot be less than used count.");

        if (nextIsPercentage && nextDiscountAmount > 100)
            throw new DomainException("COUPON_PERCENTAGE_INVALID",
                "Percentage discount cannot exceed 100%.");

        Code = nextCode;
        IsPercentage = nextIsPercentage;
        DiscountAmount = nextDiscountAmount;
        UsageLimit = nextUsageLimit;

        if (expiresAt.HasValue)
        {
            ExpiresAt = expiresAt.Value.Kind == DateTimeKind.Utc
                ? expiresAt.Value
                : expiresAt.Value.ToUniversalTime();
        }

        MarkAsUpdated();
    }

    /// <summary>
    /// Deactivate coupon.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Activate coupon.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    /// <summary>
    /// Extend expiration date.
    /// </summary>
    public void ExtendExpiration(DateTime newExpiresAt)
    {
        if (newExpiresAt <= ExpiresAt)
            throw new DomainException("COUPON_EXTEND_INVALID",
                "New expiration date must be after current expiration date.");

        ExpiresAt = newExpiresAt.Kind == DateTimeKind.Utc
            ? newExpiresAt
            : newExpiresAt.ToUniversalTime();
        MarkAsUpdated();
    }

    /// <summary>
    /// Check if coupon can be applied to a price.
    /// </summary>
    public bool CanApplyTo(decimal price)
    {
        if (!IsValid)
            return false;

        if (price <= 0)
            return false;

        return true;
    }
}
