using Platform.Domain.Common;

namespace Platform.Domain.ValueObjects;

/// <summary>
/// Progress value object (0-100 range).
/// </summary>
public sealed record Progress
{
    private const decimal MinValue = 0;
    private const decimal MaxValue = 100;

    public decimal Value { get; }
    public bool IsCompleted => Value >= MaxValue;
    public bool IsNotStarted => Value <= MinValue;
    public bool InProgress => Value > MinValue && Value < MaxValue;

    private Progress(decimal value)
    {
        Value = value;
    }

    /// <summary>
    /// Create progress with validation.
    /// </summary>
    public static Progress Of(decimal value)
    {
        Guard.Against.OutOfRange(value, MinValue, MaxValue, nameof(value));
        return new Progress(value);
    }

    /// <summary>
    /// Create zero progress.
    /// </summary>
    public static Progress Zero() => new(MinValue);

    /// <summary>
    /// Create completed progress.
    /// </summary>
    public static Progress Completed() => new(MaxValue);

    /// <summary>
    /// Add to progress.
    /// </summary>
    public Progress Add(decimal amount)
    {
        var newValue = Math.Min(Value + amount, MaxValue);
        return new Progress(newValue);
    }

    /// <summary>
    /// Set progress to specific value.
    /// </summary>
    public Progress Set(decimal value)
    {
        return Of(value);
    }

    /// <summary>
    /// Calculate percentage as string.
    /// </summary>
    public string ToPercentageString() => $"{Value:F0}%";

    /// <summary>
    /// Implicit conversion to decimal.
    /// </summary>
    public static implicit operator decimal(Progress progress) => progress.Value;

    public override string ToString() => ToPercentageString();
}
