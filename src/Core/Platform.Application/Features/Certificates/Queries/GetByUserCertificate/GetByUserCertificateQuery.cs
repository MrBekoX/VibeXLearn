using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.DTOs;

namespace Platform.Application.Features.Certificates.Queries.GetByUserCertificate;

/// <summary>
/// Query to get all certificates for a user.
/// </summary>
public sealed record GetByUserCertificateQuery(Guid UserId)
    : IRequest<Result<IList<GetByUserCertificateQueryDto>>>, ICacheableQuery
{
    public string CacheKey => CertificateCacheKeys.ByUser(UserId);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
