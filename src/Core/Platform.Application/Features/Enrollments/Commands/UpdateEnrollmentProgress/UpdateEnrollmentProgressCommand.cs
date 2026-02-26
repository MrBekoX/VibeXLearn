using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Commands.UpdateEnrollmentProgress;

/// <summary>
/// Command to update enrollment progress.
/// </summary>
public sealed record UpdateEnrollmentProgressCommand(
    Guid EnrollmentId,
    decimal Progress) : IRequest<Result>, IResolvableCacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["enrollments:*"];

    public async Task<IReadOnlyList<string>> ResolvePatternsAsync(
        IServiceProvider serviceProvider, CancellationToken ct)
    {
        var readRepo = serviceProvider.GetRequiredService<IReadRepository<Enrollment>>();
        var enrollment = await readRepo.GetByIdAsync(EnrollmentId, ct);
        if (enrollment is null)
            return CacheInvalidationPatterns;

        return
        [
            EnrollmentCacheKeys.InvalidateUser(enrollment.UserId),
            EnrollmentCacheKeys.ByCoursePattern(enrollment.CourseId),
            $"courses:id:{enrollment.CourseId}"
        ];
    }
}
