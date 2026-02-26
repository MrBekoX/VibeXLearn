using Platform.Domain.Common;
using Platform.Domain.Enums;

namespace Platform.Domain.Entities;

/// <summary>
/// Certificate with status transitions.
/// </summary>
public class Certificate : BaseEntity
{
    // Private setters for encapsulation
    public Guid              UserId          { get; private set; }
    public Guid              CourseId        { get; private set; }
    public string?           SertifierCertId { get; private set; }
    public string?           PublicUrl       { get; private set; }
    public DateTime          IssuedAt        { get; private set; }
    public CertificateStatus Status          { get; private set; } = CertificateStatus.Pending;

    // Computed properties
    public bool              IsIssued        => Status == CertificateStatus.Issued && !string.IsNullOrWhiteSpace(PublicUrl);
    public bool              IsRevoked       => Status == CertificateStatus.Revoked;

    // Navigation properties
    public AppUser           User            { get; private set; } = default!;
    public Course            Course          { get; private set; } = default!;

    // Private constructor for EF Core
    private Certificate() { }

    /// <summary>
    /// Factory method to create a pending certificate.
    /// </summary>
    public static Certificate CreatePending(Guid userId, Guid courseId)
    {
        Guard.Against.EmptyGuid(userId, nameof(userId));
        Guard.Against.EmptyGuid(courseId, nameof(courseId));

        return new Certificate
        {
            UserId = userId,
            CourseId = courseId,
            Status = CertificateStatus.Pending,
            IssuedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Mark certificate as issued with Sertifier details.
    /// </summary>
    public void MarkAsIssued(string sertifierCertId, string publicUrl)
    {
        if (Status != CertificateStatus.Pending)
            throw new DomainException("CERTIFICATE_ISSUE_INVALID_STATUS",
                "Only pending certificates can be issued.");

        Guard.Against.NullOrWhiteSpace(sertifierCertId, nameof(sertifierCertId));
        Guard.Against.NullOrWhiteSpace(publicUrl, nameof(publicUrl));

        SertifierCertId = sertifierCertId.Trim();
        PublicUrl = publicUrl.Trim();
        Status = CertificateStatus.Issued;
        IssuedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Revoke the certificate.
    /// </summary>
    public void Revoke()
    {
        if (Status == CertificateStatus.Revoked)
            throw new DomainException("CERTIFICATE_ALREADY_REVOKED",
                "Certificate is already revoked.");

        Status = CertificateStatus.Revoked;
        MarkAsUpdated();
    }

    /// <summary>
    /// Reissue a revoked certificate.
    /// </summary>
    public void Reissue()
    {
        if (Status != CertificateStatus.Revoked)
            throw new DomainException("CERTIFICATE_REISSUE_INVALID_STATUS",
                "Only revoked certificates can be reissued.");

        Status = CertificateStatus.Pending;
        SertifierCertId = null;
        PublicUrl = null;
        MarkAsUpdated();
    }
}
