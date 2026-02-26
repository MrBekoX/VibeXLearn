using Platform.Domain.Common;

namespace Platform.Domain.ValueObjects;

/// <summary>
/// Money value object with currency support.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Create Money in TRY currency.
    /// </summary>
    public static Money Try(decimal amount)
    {
        Guard.Against.Negative(amount, nameof(amount));
        return new Money(amount, "TRY");
    }

    /// <summary>
    /// Create Money in USD currency.
    /// </summary>
    public static Money Usd(decimal amount)
    {
        Guard.Against.Negative(amount, nameof(amount));
        return new Money(amount, "USD");
    }

    /// <summary>
    /// Create Money with specified currency.
    /// </summary>
    public static Money Of(decimal amount, string currency)
    {
        Guard.Against.Negative(amount, nameof(amount));
        Guard.Against.NullOrWhiteSpace(currency, nameof(currency));
        return new Money(amount, currency.ToUpperInvariant());
    }

    /// <summary>
    /// Add two Money values (same currency only).
    /// </summary>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot add money with different currencies.");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtract two Money values (same currency only).
    /// </summary>
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot subtract money with different currencies.");

        var result = Amount - other.Amount;
        if (result < 0)
            throw new DomainException("Result cannot be negative.");

        return new Money(result, Currency);
    }

    /// <summary>
    /// Apply percentage discount.
    /// </summary>
    public Money ApplyPercentageDiscount(decimal percentage)
    {
        Guard.Against.OutOfRange(percentage, 0, 100, nameof(percentage));
        var discountAmount = Amount * (percentage / 100);
        return new Money(Amount - discountAmount, Currency);
    }

    /// <summary>
    /// Apply fixed discount.
    /// </summary>
    public Money ApplyFixedDiscount(Money discount)
    {
        if (Currency != discount.Currency)
            throw new DomainException("Cannot apply discount with different currency.");

        if (discount.Amount > Amount)
            throw new DomainException("Discount cannot exceed amount.");

        return new Money(Amount - discount.Amount, Currency);
    }

    /// <summary>
    /// Check if amounts are equal (with tolerance for floating point).
    /// </summary>
    public bool AmountEquals(Money other, decimal tolerance = 0.01m)
    {
        if (Currency != other.Currency)
            return false;

        return Math.Abs(Amount - other.Amount) < tolerance;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
