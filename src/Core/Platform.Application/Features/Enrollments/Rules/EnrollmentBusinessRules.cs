using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Enrollments.Rules;

/// <summary>
/// Business rules implementation for Enrollment feature.
/// </summary>
public sealed class EnrollmentBusinessRules(
    IReadRepository<Enrollment> enrollmentRepo,
    IReadRepository<Course> courseRepo) : IEnrollmentBusinessRules
{
    public IBusinessRule EnrollmentMustExist(Guid enrollmentId)
        => new BusinessRule(
            "ENROLLMENT_NOT_FOUND",
            EnrollmentBusinessMessages.NotFoundById,
            async ct =>
            {
                var exists = await enrollmentRepo.AnyAsync(e => e.Id == enrollmentId, ct);
                return exists
                    ? Result.Success()
                    : Result.Fail(EnrollmentBusinessMessages.NotFoundById);
            });

    public IBusinessRule EnrollmentMustBeActive(Guid enrollmentId)
        => new BusinessRule(
            "ENROLLMENT_NOT_ACTIVE",
            EnrollmentBusinessMessages.NotActive,
            async ct =>
            {
                var enrollment = await enrollmentRepo.GetByIdAsync(enrollmentId, ct);
                return enrollment?.Status == EnrollmentStatus.Active
                    ? Result.Success()
                    : Result.Fail(EnrollmentBusinessMessages.NotActive);
            });

    public IBusinessRule EnrollmentMustNotBeCompleted(Guid enrollmentId)
        => new BusinessRule(
            "ENROLLMENT_ALREADY_COMPLETED",
            EnrollmentBusinessMessages.AlreadyCompleted,
            async ct =>
            {
                var enrollment = await enrollmentRepo.GetByIdAsync(enrollmentId, ct);
                return enrollment?.Status != EnrollmentStatus.Completed
                    ? Result.Success()
                    : Result.Fail(EnrollmentBusinessMessages.AlreadyCompleted);
            });

    public IBusinessRule EnrollmentMustNotBeCancelled(Guid enrollmentId)
        => new BusinessRule(
            "ENROLLMENT_ALREADY_CANCELLED",
            EnrollmentBusinessMessages.AlreadyCancelled,
            async ct =>
            {
                var enrollment = await enrollmentRepo.GetByIdAsync(enrollmentId, ct);
                return enrollment?.Status != EnrollmentStatus.Cancelled
                    ? Result.Success()
                    : Result.Fail(EnrollmentBusinessMessages.AlreadyCancelled);
            });

    public IBusinessRule UserMustNotBeEnrolled(Guid userId, Guid courseId)
        => new BusinessRule(
            "ENROLLMENT_ALREADY_EXISTS",
            EnrollmentBusinessMessages.AlreadyExists,
            async ct =>
            {
                var exists = await enrollmentRepo.AnyAsync(
                    e => e.UserId == userId && e.CourseId == courseId, ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail(EnrollmentBusinessMessages.AlreadyExists);
            });

    public IBusinessRule CourseMustBePublished(Guid courseId)
        => new BusinessRule(
            "COURSE_NOT_PUBLISHED",
            EnrollmentBusinessMessages.CourseNotPublished,
            async ct =>
            {
                var course = await courseRepo.GetByIdAsync(courseId, ct);
                return course?.Status == CourseStatus.Published
                    ? Result.Success()
                    : Result.Fail(EnrollmentBusinessMessages.CourseNotPublished);
            });

    public IBusinessRule EnrollmentMustBelongToUser(Guid enrollmentId, Guid userId)
        => new BusinessRule(
            "ENROLLMENT_NOT_BELONG_TO_USER",
            "Enrollment does not belong to this user.",
            async ct =>
            {
                var enrollment = await enrollmentRepo.GetByIdAsync(enrollmentId, ct);
                return enrollment?.UserId == userId
                    ? Result.Success()
                    : Result.Fail("Enrollment does not belong to this user.");
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
