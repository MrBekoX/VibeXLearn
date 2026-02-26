using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Certificates.DTOs;

namespace Platform.Application.Features.Certificates.Queries.GetByIdCertificate;

/// <summary>
/// Query to get a certificate by ID.
/// </summary>
public sealed record GetByIdCertificateQuery(Guid CertificateId) : IRequest<Result<GetByIdCertificateQueryDto>>;
