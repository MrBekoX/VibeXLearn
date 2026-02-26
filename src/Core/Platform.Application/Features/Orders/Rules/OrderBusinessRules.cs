using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Orders.Constants;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Orders.Rules;

/// <summary>
/// Business rules implementation for Order feature.
/// </summary>
public sealed class OrderBusinessRules(
    IReadRepository<Course> courseRepo,
    IReadRepository<Order> orderRepo,
    IReadRepository<Enrollment> enrollmentRepo,
    IReadRepository<Coupon> couponRepo) : IOrderBusinessRules
{
    public IBusinessRule CourseMustBePurchasable(Guid courseId)
        => new BusinessRule(
            "ORDER_COURSE_NOT_PURCHASABLE",
            OrderBusinessMessages.CourseNotPurchasable,
            async ct =>
            {
                var course = await courseRepo.GetByIdAsync(courseId, ct);
                return course is not null && course.Status == CourseStatus.Published
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.CourseNotPurchasable);
            });

    public IBusinessRule UserMustNotBeEnrolled(Guid userId, Guid courseId)
        => new BusinessRule(
            "ORDER_ALREADY_ENROLLED",
            OrderBusinessMessages.AlreadyEnrolled,
            async ct =>
            {
                var exists = await enrollmentRepo.AnyAsync(
                    e => e.UserId == userId && e.CourseId == courseId, ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.AlreadyEnrolled);
            });

    public IBusinessRule NoPendingOrderExists(Guid userId, Guid courseId)
        => new BusinessRule(
            "ORDER_PENDING_EXISTS",
            OrderBusinessMessages.PendingOrderExists,
            async ct =>
            {
                var exists = await orderRepo.AnyAsync(
                    o => o.UserId == userId &&
                         o.CourseId == courseId &&
                         o.Status == OrderStatus.Pending, ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.PendingOrderExists);
            });

    public IBusinessRule OrderMustExist(Guid orderId)
        => new BusinessRule(
            "ORDER_NOT_FOUND",
            OrderBusinessMessages.NotFoundById,
            async ct =>
            {
                var exists = await orderRepo.AnyAsync(o => o.Id == orderId, ct);
                return exists
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.NotFoundById);
            });

    public IBusinessRule OrderMustBelongToUser(Guid orderId, Guid userId)
        => new BusinessRule(
            "ORDER_NOT_BELONG_TO_USER",
            OrderBusinessMessages.OrderNotBelongToUser,
            async ct =>
            {
                var order = await orderRepo.GetByIdAsync(orderId, ct);
                return order?.UserId == userId
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.OrderNotBelongToUser);
            });

    public IBusinessRule OrderMustBeCreated(Guid orderId)
        => new BusinessRule(
            "ORDER_NOT_CREATED",
            OrderBusinessMessages.NotCreated,
            async ct =>
            {
                var order = await orderRepo.GetByIdAsync(orderId, ct);
                return order?.Status == OrderStatus.Created
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.NotCreated);
            });

    public IBusinessRule OrderMustBePending(Guid orderId)
        => new BusinessRule(
            "ORDER_NOT_PENDING",
            OrderBusinessMessages.NotPending,
            async ct =>
            {
                var order = await orderRepo.GetByIdAsync(orderId, ct);
                return order?.Status == OrderStatus.Pending
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.NotPending);
            });

    public IBusinessRule CouponMustBeValid(string couponCode)
        => new BusinessRule(
            "ORDER_COUPON_NOT_FOUND",
            OrderBusinessMessages.CouponNotFound,
            async ct =>
            {
                var normalized = couponCode.ToUpperInvariant();
                var exists = await couponRepo.AnyAsync(c => c.Code.ToUpper() == normalized, ct);
                return exists
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.CouponNotFound);
            });

    public IBusinessRule CouponMustBeActive(string couponCode)
        => new BusinessRule(
            "ORDER_COUPON_NOT_ACTIVE",
            OrderBusinessMessages.CouponNotApplicable,
            async ct =>
            {
                var normalized = couponCode.ToUpperInvariant();
                var coupon = await couponRepo.GetAsync(
                    c => c.Code.ToUpper() == normalized, ct);
                return coupon?.IsActive == true
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.CouponNotApplicable);
            });

    public IBusinessRule CouponMustNotBeExpired(string couponCode)
        => new BusinessRule(
            "ORDER_COUPON_EXPIRED",
            OrderBusinessMessages.CouponExpired,
            async ct =>
            {
                var normalized = couponCode.ToUpperInvariant();
                var coupon = await couponRepo.GetAsync(
                    c => c.Code.ToUpper() == normalized, ct);
                return coupon is not null && coupon.ExpiresAt > DateTime.UtcNow
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.CouponExpired);
            });

    public IBusinessRule CouponUsageLimitMustNotBeExceeded(string couponCode)
        => new BusinessRule(
            "ORDER_COUPON_LIMIT_EXCEEDED",
            OrderBusinessMessages.CouponUsageLimitExceeded,
            async ct =>
            {
                var normalized = couponCode.ToUpperInvariant();
                var coupon = await couponRepo.GetAsync(
                    c => c.Code.ToUpper() == normalized, ct);
                return coupon is not null && coupon.UsedCount < coupon.UsageLimit
                    ? Result.Success()
                    : Result.Fail(OrderBusinessMessages.CouponUsageLimitExceeded);
            });
}

/// <summary>
/// Simple business rule implementation.
/// </summary>
file sealed class BusinessRule(string code, string message, Func<CancellationToken, Task<Result>> checkFunc)
    : IBusinessRule
{
    public string Code => code;
    public string Message => message;
    public Task<Result> CheckAsync(CancellationToken ct) => checkFunc(ct);
}
