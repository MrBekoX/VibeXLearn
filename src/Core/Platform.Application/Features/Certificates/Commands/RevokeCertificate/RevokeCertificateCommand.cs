using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Certificates.Commands.RevokeCertificate;

/// <summary>
/// Command to revoke a certificate.
/// </summary>
public sealed record RevokeCertificateCommand(Guid CertificateId) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["certificates:*"];
}
