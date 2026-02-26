using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Queries.GetByIdEnrollment;

/// <summary>
/// Handler for GetByIdEnrollmentQuery.
/// </summary>
public sealed class GetByIdEnrollmentQueryHandler(
    IReadRepository<Enrollment> readRepo) : IRequestHandler<GetByIdEnrollmentQuery, Result<GetByIdEnrollmentQueryDto>>
{
    public async Task<Result<GetByIdEnrollmentQueryDto>> Handle(
        GetByIdEnrollmentQuery request, CancellationToken ct)
    {

        // Get enrollment with includes
        var enrollment = await readRepo.GetAsync(
            predicate: e => e.Id == request.EnrollmentId,
            ct: ct,
            includes: [e => e.Course]);

        if (enrollment is null)
            return Result.Fail<GetByIdEnrollmentQueryDto>("ENROLLMENT_NOT_FOUND", EnrollmentBusinessMessages.NotFoundById);

        // Map to DTO
        var dto = new GetByIdEnrollmentQueryDto
        {
            Id = enrollment.Id,
            UserId = enrollment.UserId,
            CourseId = enrollment.CourseId,
            CourseTitle = enrollment.Course?.Title ?? string.Empty,
            CourseThumbnailUrl = enrollment.Course?.ThumbnailUrl,
            Status = enrollment.Status.ToString(),
            Progress = enrollment.Progress,
            EnrolledAt = enrollment.CreatedAt,
            CompletedAt = enrollment.CompletedAt
        };

        return Result.Success(dto);
    }
}
