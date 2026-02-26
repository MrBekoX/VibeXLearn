using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Orders.Rules;

/// <summary>
/// Business rules interface for Order feature.
/// </summary>
public interface IOrderBusinessRules
{
    IBusinessRule CourseMustBePurchasable(Guid courseId);
    IBusinessRule UserMustNotBeEnrolled(Guid userId, Guid courseId);
    IBusinessRule NoPendingOrderExists(Guid userId, Guid courseId);
    IBusinessRule OrderMustExist(Guid orderId);
    IBusinessRule OrderMustBelongToUser(Guid orderId, Guid userId);
    IBusinessRule OrderMustBeCreated(Guid orderId);
    IBusinessRule OrderMustBePending(Guid orderId);
    IBusinessRule CouponMustBeValid(string couponCode);
    IBusinessRule CouponMustBeActive(string couponCode);
    IBusinessRule CouponMustNotBeExpired(string couponCode);
    IBusinessRule CouponUsageLimitMustNotBeExceeded(string couponCode);
}
