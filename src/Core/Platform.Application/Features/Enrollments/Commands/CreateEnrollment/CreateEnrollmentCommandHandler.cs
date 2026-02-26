using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Commands.CreateEnrollment;

/// <summary>
/// Handler for CreateEnrollmentCommand.
/// </summary>
public sealed class CreateEnrollmentCommandHandler(
    IReadRepository<Course> courseReadRepo,
    IWriteRepository<Enrollment> writeRepo,
    IWriteRepository<Course> courseWriteRepo,
    IEnrollmentBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CreateEnrollmentCommandHandler> logger) : IRequestHandler<CreateEnrollmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEnrollmentCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CourseMustBePublished(request.CourseId),
            rules.UserMustNotBeEnrolled(request.UserId, request.CourseId));

        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        // Create enrollment using domain factory
        var enrollment = Enrollment.Create(request.UserId, request.CourseId);
        var course = await courseReadRepo.GetByIdAsync(request.CourseId, ct, tracking: true);
        if (course is null)
            return Result.Fail<Guid>("COURSE_NOT_FOUND", EnrollmentBusinessMessages.CourseNotPublished);

        course.IncrementEnrollment();

        await writeRepo.AddAsync(enrollment, ct);
        await courseWriteRepo.UpdateAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment created: {EnrollmentId} for User: {UserId} in Course: {CourseId}",
            enrollment.Id, request.UserId, request.CourseId);

        return Result.Success(enrollment.Id);
    }
}
