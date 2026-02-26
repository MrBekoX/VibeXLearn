namespace Platform.Application.Features.Certificates.Constants;

/// <summary>
/// Certificate validation messages.
/// </summary>
public static class CertificateValidationMessages
{
    // ID
    public const string CertificateIdRequired = "Certificate ID is required.";
    public const string CertificateIdEmpty = "Certificate ID cannot be empty.";

    // User ID
    public const string UserIdRequired = "User ID is required.";
    public const string UserIdEmpty = "User ID cannot be empty.";

    // Course ID
    public const string CourseIdRequired = "Course ID is required.";
    public const string CourseIdEmpty = "Course ID cannot be empty.";

    // Sertifier ID
    public const string SertifierCertIdRequired = "Sertifier certificate ID is required.";
    public const string SertifierCertIdMaxLength = "Sertifier certificate ID cannot exceed 200 characters.";

    // Public URL
    public const string PublicUrlRequired = "Public URL is required.";
    public const string PublicUrlMaxLength = "Public URL cannot exceed 500 characters.";
    public const string PublicUrlInvalidFormat = "Public URL must be a valid URL.";
}
