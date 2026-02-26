using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Certificates.Queries.GetByCourseCertificate;

/// <summary>
/// Handler for GetByCourseCertificateQuery.
/// </summary>
public sealed class GetByCourseCertificateQueryHandler(
    IReadRepository<Certificate> repo) : IRequestHandler<GetByCourseCertificateQuery, Result<IList<GetAllCertificatesQueryDto>>>
{
    public async Task<Result<IList<GetAllCertificatesQueryDto>>> Handle(
        GetByCourseCertificateQuery request,
        CancellationToken ct)
    {

        var certificates = await repo.GetListAsync(
            c => c.CourseId == request.CourseId,
            ct,
            includes: [c => c.User, c => c.Course]);

        var dtos = certificates.Select(c => new GetAllCertificatesQueryDto
        {
            Id = c.Id,
            UserId = c.UserId,
            UserName = c.User != null ? $"{c.User.FirstName} {c.User.LastName}" : string.Empty,
            CourseId = c.CourseId,
            CourseTitle = c.Course?.Title ?? string.Empty,
            Status = c.Status.ToString(),
            IssuedAt = c.IssuedAt
        }).ToList();

        IList<GetAllCertificatesQueryDto> result = dtos;

        return Result.Success(result);
    }
}
