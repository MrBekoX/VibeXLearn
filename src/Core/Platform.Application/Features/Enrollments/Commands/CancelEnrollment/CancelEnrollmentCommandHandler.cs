using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Commands.CancelEnrollment;

/// <summary>
/// Handler for CancelEnrollmentCommand.
/// </summary>
public sealed class CancelEnrollmentCommandHandler(
    IReadRepository<Enrollment> readRepo,
    IWriteRepository<Enrollment> writeRepo,
    IEnrollmentBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CancelEnrollmentCommandHandler> logger) : IRequestHandler<CancelEnrollmentCommand, Result>
{
    public async Task<Result> Handle(CancelEnrollmentCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.EnrollmentMustExist(request.EnrollmentId),
            rules.EnrollmentMustNotBeCompleted(request.EnrollmentId),
            rules.EnrollmentMustNotBeCancelled(request.EnrollmentId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get enrollment with tracking
        var enrollment = await readRepo.GetByIdAsync(request.EnrollmentId, ct, tracking: true);
        if (enrollment is null)
            return Result.Fail("ENROLLMENT_NOT_FOUND", EnrollmentBusinessMessages.NotFoundById);

        // Cancel using domain method
        enrollment.Cancel();

        await writeRepo.UpdateAsync(enrollment, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment cancelled: {EnrollmentId}", enrollment.Id);

        return Result.Success();
    }
}
