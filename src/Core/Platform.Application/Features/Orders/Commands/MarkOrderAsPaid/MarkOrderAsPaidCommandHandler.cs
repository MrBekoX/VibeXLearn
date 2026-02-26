using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Commands.MarkOrderAsPaid;

/// <summary>
/// Handler for MarkOrderAsPaidCommand.
/// </summary>
public sealed class MarkOrderAsPaidCommandHandler(
    IReadRepository<Order> readRepo,
    IReadRepository<Course> courseReadRepo,
    IReadRepository<Coupon> couponReadRepo,
    IWriteRepository<Order> writeRepo,
    IWriteRepository<Course> courseWriteRepo,
    IWriteRepository<Coupon> couponWriteRepo,
    IWriteRepository<Enrollment> enrollmentWriteRepo,
    IOrderBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<MarkOrderAsPaidCommandHandler> logger) : IRequestHandler<MarkOrderAsPaidCommand, Result>
{
    public async Task<Result> Handle(MarkOrderAsPaidCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.OrderMustExist(request.OrderId),
            rules.OrderMustBePending(request.OrderId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get order with tracking
        var order = await readRepo.GetByIdAsync(request.OrderId, ct, tracking: true);
        if (order is null)
            return Result.Fail("ORDER_NOT_FOUND", OrderBusinessMessages.NotFoundById);

        var course = await courseReadRepo.GetByIdAsync(order.CourseId, ct, tracking: true);
        if (course is null)
            return Result.Fail("COURSE_NOT_FOUND", "Course not found.");

        Coupon? coupon = null;
        if (order.CouponId.HasValue)
        {
            coupon = await couponReadRepo.GetByIdAsync(order.CouponId.Value, ct, tracking: true);
            if (coupon is null)
                return Result.Fail("COUPON_NOT_FOUND", OrderBusinessMessages.CouponNotFound);
        }

        // Atomically update order and create enrollment
        await uow.ExecuteInTransactionAsync(async () =>
        {
            // Mark order as paid using domain method
            order.MarkAsPaid();
            course.IncrementEnrollment();
            coupon?.RecordUsage();

            await writeRepo.UpdateAsync(order, ct);
            await courseWriteRepo.UpdateAsync(course, ct);
            if (coupon is not null)
                await couponWriteRepo.UpdateAsync(coupon, ct);

            // Create enrollment
            var enrollment = Enrollment.Create(order.UserId, order.CourseId);
            await enrollmentWriteRepo.AddAsync(enrollment, ct);

            await uow.SaveChangesAsync(ct);
        }, ct);

        logger.LogInformation("Order marked as paid: {OrderId}, Enrollment created for User: {UserId}",
            order.Id, order.UserId);

        return Result.Success();
    }
}
