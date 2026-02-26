using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Certificates.Commands.CreatePendingCertificate;

/// <summary>
/// Command to create a pending certificate for a user and course.
/// </summary>
public sealed record CreatePendingCertificateCommand(
    Guid UserId,
    Guid CourseId) : IRequest<Result<Guid>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["certificates:*"];
}
