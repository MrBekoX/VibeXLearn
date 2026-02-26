using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Handler for CreateOrderCommand.
/// </summary>
public sealed class CreateOrderCommandHandler(
    IReadRepository<Course> courseRepo,
    IReadRepository<Coupon> couponRepo,
    IWriteRepository<Order> writeRepo,
    IOrderBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CourseMustBePurchasable(request.CourseId),
            rules.UserMustNotBeEnrolled(request.UserId, request.CourseId),
            rules.NoPendingOrderExists(request.UserId, request.CourseId));

        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        // Get course for price
        var course = await courseRepo.GetByIdAsync(request.CourseId, ct);
        if (course is null)
            return Result.Fail<Guid>("COURSE_NOT_FOUND", "Course not found.");

        // Create order from current course price.
        var order = Order.Create(request.UserId, request.CourseId, course.Price);

        // Apply coupon if provided.
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var couponResult = await ruleEngine.RunAsync(ct,
                rules.CouponMustBeValid(request.CouponCode),
                rules.CouponMustBeActive(request.CouponCode),
                rules.CouponMustNotBeExpired(request.CouponCode),
                rules.CouponUsageLimitMustNotBeExceeded(request.CouponCode));

            if (couponResult.IsFailure)
                return Result.Fail<Guid>(couponResult.Error);

            var normalized = request.CouponCode.ToUpperInvariant();
            var coupon = await couponRepo.GetAsync(c => c.Code.ToUpper() == normalized, ct);
            if (coupon is null)
                return Result.Fail<Guid>("COUPON_NOT_FOUND", OrderBusinessMessages.CouponNotFound);

            order.ApplyCoupon(coupon);
        }

        await writeRepo.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Order created: {OrderId} for Course: {CourseId} by User: {UserId}",
            order.Id, request.CourseId, request.UserId);

        return Result.Success(order.Id);
    }
}
