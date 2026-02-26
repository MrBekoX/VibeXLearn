using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Certificates.Queries.GetByUserCertificate;

/// <summary>
/// Handler for GetByUserCertificateQuery.
/// </summary>
public sealed class GetByUserCertificateQueryHandler(
    IReadRepository<Certificate> repo) : IRequestHandler<GetByUserCertificateQuery, Result<IList<GetByUserCertificateQueryDto>>>
{
    public async Task<Result<IList<GetByUserCertificateQueryDto>>> Handle(
        GetByUserCertificateQuery request,
        CancellationToken ct)
    {

        var certificates = await repo.GetListAsync(
            c => c.UserId == request.UserId,
            ct,
            includes: [c => c.Course]);

        var dtos = certificates.Select(c => new GetByUserCertificateQueryDto
        {
            Id = c.Id,
            CourseId = c.CourseId,
            CourseTitle = c.Course?.Title ?? string.Empty,
            SertifierCertId = c.SertifierCertId,
            PublicUrl = c.PublicUrl,
            Status = c.Status.ToString(),
            IssuedAt = c.IssuedAt
        }).ToList();

        IList<GetByUserCertificateQueryDto> result = dtos;

        return Result.Success(result);
    }
}
