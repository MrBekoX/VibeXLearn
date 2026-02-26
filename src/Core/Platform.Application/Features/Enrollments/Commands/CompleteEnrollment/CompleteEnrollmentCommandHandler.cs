using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Commands.CompleteEnrollment;

/// <summary>
/// Handler for CompleteEnrollmentCommand.
/// </summary>
public sealed class CompleteEnrollmentCommandHandler(
    IReadRepository<Enrollment> readRepo,
    IWriteRepository<Enrollment> writeRepo,
    IEnrollmentBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CompleteEnrollmentCommandHandler> logger) : IRequestHandler<CompleteEnrollmentCommand, Result>
{
    public async Task<Result> Handle(CompleteEnrollmentCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.EnrollmentMustExist(request.EnrollmentId),
            rules.EnrollmentMustBeActive(request.EnrollmentId),
            rules.EnrollmentMustNotBeCompleted(request.EnrollmentId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get enrollment with tracking
        var enrollment = await readRepo.GetByIdAsync(request.EnrollmentId, ct, tracking: true);
        if (enrollment is null)
            return Result.Fail("ENROLLMENT_NOT_FOUND", EnrollmentBusinessMessages.NotFoundById);

        // Mark as completed using domain method
        enrollment.Complete();

        await writeRepo.UpdateAsync(enrollment, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment completed: {EnrollmentId}", enrollment.Id);

        return Result.Success();
    }
}
