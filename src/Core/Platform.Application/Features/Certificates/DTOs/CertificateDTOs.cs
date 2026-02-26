namespace Platform.Application.Features.Certificates.DTOs;

/// <summary>
/// DTO for certificate details.
/// </summary>
public sealed record GetByIdCertificateQueryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = default!;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? SertifierCertId { get; init; }
    public string? PublicUrl { get; init; }
    public string Status { get; init; } = default!;
    public DateTime IssuedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for certificate list items.
/// </summary>
public sealed record GetAllCertificatesQueryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = default!;
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTime IssuedAt { get; init; }
}

/// <summary>
/// DTO for certificate creation request.
/// </summary>
public sealed record CreatePendingCertificateCommandDto
{
    public Guid UserId { get; init; }
    public Guid CourseId { get; init; }
}

/// <summary>
/// DTO for marking certificate as issued request.
/// </summary>
public sealed record MarkCertificateAsIssuedCommandDto
{
    public Guid CertificateId { get; init; }
    public string SertifierCertId { get; init; } = default!;
    public string PublicUrl { get; init; } = default!;
}

/// <summary>
/// DTO for user's certificate list.
/// </summary>
public sealed record GetByUserCertificateQueryDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = default!;
    public string? SertifierCertId { get; init; }
    public string? PublicUrl { get; init; }
    public string Status { get; init; } = default!;
    public DateTime IssuedAt { get; init; }
}
