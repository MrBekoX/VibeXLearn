using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Commands.UpdateEnrollmentProgress;

/// <summary>
/// Handler for UpdateEnrollmentProgressCommand.
/// </summary>
public sealed class UpdateEnrollmentProgressCommandHandler(
    IReadRepository<Enrollment> readRepo,
    IWriteRepository<Enrollment> writeRepo,
    IEnrollmentBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<UpdateEnrollmentProgressCommandHandler> logger) : IRequestHandler<UpdateEnrollmentProgressCommand, Result>
{
    public async Task<Result> Handle(UpdateEnrollmentProgressCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.EnrollmentMustExist(request.EnrollmentId),
            rules.EnrollmentMustBeActive(request.EnrollmentId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get enrollment with tracking
        var enrollment = await readRepo.GetByIdAsync(request.EnrollmentId, ct, tracking: true);
        if (enrollment is null)
            return Result.Fail("ENROLLMENT_NOT_FOUND", EnrollmentBusinessMessages.NotFoundById);

        // Update progress using domain method
        enrollment.UpdateProgress(request.Progress);

        await writeRepo.UpdateAsync(enrollment, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment progress updated: {EnrollmentId} -> {Progress}%",
            enrollment.Id, request.Progress);

        return Result.Success();
    }
}
