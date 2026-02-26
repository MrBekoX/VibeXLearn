using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Certificates.Commands.MarkCertificateAsIssued;

/// <summary>
/// Command to mark a certificate as issued with Sertifier details.
/// </summary>
public sealed record MarkCertificateAsIssuedCommand(
    Guid CertificateId,
    string SertifierCertId,
    string PublicUrl) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["certificates:*"];
}
