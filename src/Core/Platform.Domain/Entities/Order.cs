using Platform.Domain.Common;
using Platform.Domain.Enums;
using Platform.Domain.Events;

namespace Platform.Domain.Entities;

/// <summary>
/// Order aggregate with state machine for payment flow.
/// </summary>
public class Order : AuditableEntity, IAggregateRoot
{
    // Private setters for encapsulation
    public Guid        UserId         { get; private set; }
    public Guid        CourseId       { get; private set; }
    public decimal     Amount         { get; private set; }
    public string      Currency       { get; private set; } = "TRY";
    public OrderStatus Status         { get; private set; } = OrderStatus.Created;
    public Guid?       CouponId       { get; private set; }
    public decimal?    DiscountAmount { get; private set; }
    public decimal     FinalAmount    => DiscountAmount.HasValue ? Amount - DiscountAmount.Value : Amount;

    // Navigation properties
    public AppUser        User          { get; private set; } = default!;
    public Course         Course        { get; private set; } = default!;
    public Coupon?        Coupon        { get; private set; }
    public PaymentIntent? PaymentIntent { get; private set; }

    // Private constructor for EF Core
    private Order() { }

    /// <summary>
    /// Factory method to create a new order.
    /// </summary>
    public static Order Create(Guid userId, Guid courseId, decimal amount, string currency = "TRY")
    {
        Guard.Against.EmptyGuid(userId, nameof(userId));
        Guard.Against.EmptyGuid(courseId, nameof(courseId));
        Guard.Against.NegativeOrZero(amount, nameof(amount));

        var order = new Order
        {
            UserId = userId,
            CourseId = courseId,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Status = OrderStatus.Created
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, userId, courseId, amount));
        return order;
    }

    /// <summary>
    /// Apply coupon to the order. Only works on created orders.
    /// </summary>
    public void ApplyCoupon(Coupon coupon)
    {
        if (Status != OrderStatus.Created)
            throw new DomainException("ORDER_COUPON_INVALID_STATUS",
                "Coupon can only be applied to created orders.");

        if (!coupon.IsValid)
            throw new DomainException("ORDER_COUPON_INVALID",
                "Coupon is not valid or has expired.");

        if (CouponId.HasValue)
            throw new DomainException("ORDER_COUPON_ALREADY_APPLIED",
                "A coupon has already been applied to this order.");

        CouponId = coupon.Id;
        DiscountAmount = coupon.CalculateDiscount(Amount);
        MarkAsUpdated();
    }

    /// <summary>
    /// Remove coupon from the order.
    /// </summary>
    public void RemoveCoupon()
    {
        if (Status != OrderStatus.Created)
            throw new DomainException("ORDER_COUPON_REMOVE_INVALID_STATUS",
                "Cannot remove coupon from order that is not in created status.");

        CouponId = null;
        DiscountAmount = null;
        MarkAsUpdated();
    }

    /// <summary>
    /// Transition order to pending state (checkout initiated).
    /// </summary>
    public void MarkAsPending()
    {
        if (Status != OrderStatus.Created)
            throw new DomainException("ORDER_PENDING_INVALID_STATUS",
                "Order must be in Created status to become pending.");

        Status = OrderStatus.Pending;
        MarkAsUpdated();
    }

    /// <summary>
    /// Mark order as paid. Only pending orders can be paid.
    /// </summary>
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("ORDER_PAID_INVALID_STATUS",
                "Order must be in Pending status to be marked as paid.");

        Status = OrderStatus.Paid;
        MarkAsUpdated();
        AddDomainEvent(new OrderPaidEvent(Id, UserId, CourseId));
    }

    /// <summary>
    /// Mark order as failed.
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        if (Status == OrderStatus.Paid)
            throw new DomainException("ORDER_FAIL_INVALID_STATUS",
                "Cannot fail a paid order.");

        Status = OrderStatus.Failed;
        MarkAsUpdated();
        AddDomainEvent(new OrderFailedEvent(Id, reason));
    }

    /// <summary>
    /// Mark order as refunded (for Faz-2).
    /// </summary>
    public void MarkAsRefunded()
    {
        if (Status != OrderStatus.Paid)
            throw new DomainException("ORDER_REFUND_INVALID_STATUS",
                "Only paid orders can be refunded.");

        Status = OrderStatus.Refunded;
        MarkAsUpdated();
    }

    /// <summary>
    /// Check if order can be paid.
    /// </summary>
    public bool CanBePaid => Status == OrderStatus.Created || Status == OrderStatus.Pending;

    /// <summary>
    /// Check if order is completed.
    /// </summary>
    public bool IsCompleted => Status == OrderStatus.Paid;

    /// <summary>
    /// Check if order has discount.
    /// </summary>
    public bool HasDiscount => DiscountAmount.HasValue && DiscountAmount.Value > 0;
}
