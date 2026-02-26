using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Certificates.Queries.GetByIdCertificate;

/// <summary>
/// Handler for GetByIdCertificateQuery.
/// </summary>
public sealed class GetByIdCertificateQueryHandler(
    IReadRepository<Certificate> repo) : IRequestHandler<GetByIdCertificateQuery, Result<GetByIdCertificateQueryDto>>
{
    public async Task<Result<GetByIdCertificateQueryDto>> Handle(
        GetByIdCertificateQuery request,
        CancellationToken ct)
    {

        var certificate = await repo.GetAsync(
            c => c.Id == request.CertificateId, ct,
            includes: [c => c.User, c => c.Course]);

        if (certificate is null)
            return Result.Fail<GetByIdCertificateQueryDto>("CERTIFICATE_NOT_FOUND", CertificateBusinessMessages.NotFoundById);

        var dto = new GetByIdCertificateQueryDto
        {
            Id = certificate.Id,
            UserId = certificate.UserId,
            UserName = certificate.User != null ? $"{certificate.User.FirstName} {certificate.User.LastName}" : string.Empty,
            CourseId = certificate.CourseId,
            CourseTitle = certificate.Course?.Title ?? string.Empty,
            SertifierCertId = certificate.SertifierCertId,
            PublicUrl = certificate.PublicUrl,
            Status = certificate.Status.ToString(),
            IssuedAt = certificate.IssuedAt,
            CreatedAt = certificate.CreatedAt
        };
        return Result.Success(dto);
    }
}
