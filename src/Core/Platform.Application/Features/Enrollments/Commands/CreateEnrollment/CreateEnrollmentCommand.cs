using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Enrollments.Commands.CreateEnrollment;

/// <summary>
/// Command to create a new enrollment (typically called after payment success).
/// </summary>
public sealed record CreateEnrollmentCommand(
    Guid UserId,
    Guid CourseId) : IRequest<Result<Guid>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns =>
    [
        $"enrollments:user:{UserId}:*",
        $"enrollments:course:{CourseId}:*",
        $"courses:id:{CourseId}"  // enrollment count denormalized on course
    ];
}
