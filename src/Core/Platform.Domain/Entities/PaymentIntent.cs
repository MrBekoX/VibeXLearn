using Platform.Domain.Common;
using Platform.Domain.Enums;
using Platform.Domain.Events;

namespace Platform.Domain.Entities;

/// <summary>
/// PaymentIntent with state machine for Iyzico payment flow.
/// </summary>
public class PaymentIntent : BaseEntity, IAggregateRoot
{
    // Private setters for encapsulation
    public Guid          OrderId             { get; private set; }
    public string        ConversationId      { get; private set; } = default!;
    public string?       IyzicoToken         { get; private set; }
    public string?       IyzicoPaymentId     { get; private set; }
    public decimal       ExpectedPrice       { get; private set; }
    public string        Currency            { get; private set; } = "TRY";
    public PaymentStatus Status              { get; private set; } = PaymentStatus.Created;
    public string?       FailReason          { get; private set; }
    public string?       RawCallbackSnapshot { get; private set; }

    // Navigation properties
    public Order         Order               { get; private set; } = default!;

    // Private constructor for EF Core
    private PaymentIntent() { }

    /// <summary>
    /// Factory method to create a new payment intent.
    /// </summary>
    public static PaymentIntent Create(
        Guid orderId,
        string conversationId,
        decimal expectedPrice,
        string currency = "TRY")
    {
        Guard.Against.EmptyGuid(orderId, nameof(orderId));
        Guard.Against.NullOrWhiteSpace(conversationId, nameof(conversationId));
        Guard.Against.NegativeOrZero(expectedPrice, nameof(expectedPrice));

        return new PaymentIntent
        {
            OrderId = orderId,
            ConversationId = conversationId,
            ExpectedPrice = expectedPrice,
            Currency = currency.ToUpperInvariant(),
            Status = PaymentStatus.Created
        };
    }

    /// <summary>
    /// Mark as processing when Iyzico checkout is initiated.
    /// </summary>
    public void MarkAsProcessing(string iyzicoToken)
    {
        if (Status != PaymentStatus.Created)
            throw new DomainException("PAYMENT_PROCESSING_INVALID_STATUS",
                "Payment intent must be in Created status to start processing.");

        Guard.Against.NullOrWhiteSpace(iyzicoToken, nameof(iyzicoToken));

        Status = PaymentStatus.Processing;
        IyzicoToken = iyzicoToken;
        MarkAsUpdated();
        AddDomainEvent(new PaymentProcessingEvent(OrderId, ConversationId));
    }

    /// <summary>
    /// Mark as completed when payment is successful.
    /// </summary>
    public void MarkAsCompleted(string paymentId)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException("PAYMENT_COMPLETED_INVALID_STATUS",
                "Payment intent must be in Processing status to complete.");

        Guard.Against.NullOrWhiteSpace(paymentId, nameof(paymentId));

        Status = PaymentStatus.Completed;
        IyzicoPaymentId = paymentId;
        FailReason = null;
        MarkAsUpdated();
        AddDomainEvent(new PaymentCompletedEvent(OrderId, paymentId));
    }

    /// <summary>
    /// Mark as failed when payment fails.
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        if (Status == PaymentStatus.Completed)
            throw new DomainException("PAYMENT_FAIL_COMPLETED",
                "Cannot fail a completed payment.");

        Status = PaymentStatus.Failed;
        FailReason = reason;
        MarkAsUpdated();
        AddDomainEvent(new PaymentFailedEvent(OrderId, reason));
    }

    /// <summary>
    /// Cancel the payment intent.
    /// </summary>
    public void Cancel()
    {
        if (Status == PaymentStatus.Completed)
            throw new DomainException("PAYMENT_CANCEL_COMPLETED",
                "Cannot cancel a completed payment.");

        Status = PaymentStatus.Cancelled;
        MarkAsUpdated();
    }

    /// <summary>
    /// Store raw callback snapshot for audit.
    /// </summary>
    public void SetRawCallbackSnapshot(string snapshot)
    {
        RawCallbackSnapshot = snapshot;
    }

    /// <summary>
    /// Verify price matches expected price (price tampering check).
    /// </summary>
    public bool VerifyPrice(decimal actualPrice, decimal tolerance = 0.01m)
    {
        return Math.Abs(ExpectedPrice - actualPrice) <= tolerance;
    }

    /// <summary>
    /// Check if payment is processed.
    /// </summary>
    public bool IsProcessed => Status == PaymentStatus.Completed;

    /// <summary>
    /// Check if payment can be retried.
    /// </summary>
    public bool CanRetry => Status == PaymentStatus.Failed || Status == PaymentStatus.Cancelled;

    /// <summary>
    /// Check if payment is terminal (no further state changes).
    /// </summary>
    public bool IsTerminal => Status == PaymentStatus.Completed ||
                              Status == PaymentStatus.Cancelled;
}
