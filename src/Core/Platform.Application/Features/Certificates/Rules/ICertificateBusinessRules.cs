using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Certificates.Rules;

/// <summary>
/// Certificate business rules interface.
/// </summary>
public interface ICertificateBusinessRules
{
    /// <summary>
    /// Rule: Certificate must exist in the system.
    /// </summary>
    IBusinessRule CertificateMustExist(Guid certificateId);

    /// <summary>
    /// Rule: Certificate must be in Pending status.
    /// </summary>
    IBusinessRule CertificateMustBePending(Guid certificateId);

    /// <summary>
    /// Rule: Certificate must not be revoked.
    /// </summary>
    IBusinessRule CertificateMustNotBeRevoked(Guid certificateId);

    /// <summary>
    /// Rule: User must not already have a certificate for the course.
    /// </summary>
    IBusinessRule UserMustNotHaveCertificateForCourse(Guid userId, Guid courseId);
}
