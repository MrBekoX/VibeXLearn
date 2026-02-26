using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Enrollments.Rules;

/// <summary>
/// Business rules interface for Enrollment feature.
/// </summary>
public interface IEnrollmentBusinessRules
{
    IBusinessRule EnrollmentMustExist(Guid enrollmentId);
    IBusinessRule EnrollmentMustBeActive(Guid enrollmentId);
    IBusinessRule EnrollmentMustNotBeCompleted(Guid enrollmentId);
    IBusinessRule EnrollmentMustNotBeCancelled(Guid enrollmentId);
    IBusinessRule UserMustNotBeEnrolled(Guid userId, Guid courseId);
    IBusinessRule CourseMustBePublished(Guid courseId);
    IBusinessRule EnrollmentMustBelongToUser(Guid enrollmentId, Guid userId);
}
