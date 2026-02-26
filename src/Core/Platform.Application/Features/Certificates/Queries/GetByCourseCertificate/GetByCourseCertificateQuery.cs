using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Certificates.DTOs;

namespace Platform.Application.Features.Certificates.Queries.GetByCourseCertificate;

/// <summary>
/// Query to get all certificates for a course.
/// </summary>
public sealed record GetByCourseCertificateQuery(Guid CourseId) : IRequest<Result<IList<GetAllCertificatesQueryDto>>>;
