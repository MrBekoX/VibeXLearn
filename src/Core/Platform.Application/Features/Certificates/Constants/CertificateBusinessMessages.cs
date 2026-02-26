namespace Platform.Application.Features.Certificates.Constants;

/// <summary>
/// Certificate business rule messages.
/// </summary>
public static class CertificateBusinessMessages
{
    // Not Found
    public const string NotFound = "Certificate not found.";
    public const string NotFoundById = "Certificate not found with the specified ID.";

    // Status Errors
    public const string AlreadyIssued = "Certificate has already been issued.";
    public const string AlreadyRevoked = "Certificate has already been revoked.";
    public const string NotPending = "Only pending certificates can be issued.";
    public const string CannotRevokeIssued = "Cannot revoke an already issued certificate.";
    public const string NotIssued = "Only issued certificates can be downloaded.";

    // Validation Errors
    public const string UserAlreadyHasCertificate = "User already has a certificate for this course.";
    public const string CourseNotCompleted = "Course must be completed before issuing certificate.";
    public const string EnrollmentNotFound = "Enrollment not found for user and course.";

    // Success Messages
    public const string CreatedSuccessfully = "Certificate pending creation started.";
    public const string IssuedSuccessfully = "Certificate issued successfully.";
    public const string RevokedSuccessfully = "Certificate revoked successfully.";
}
