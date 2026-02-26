using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Enrollments.DTOs;

namespace Platform.Application.Features.Enrollments.Queries.GetByIdEnrollment;

/// <summary>
/// Query to get enrollment by ID.
/// </summary>
public sealed record GetByIdEnrollmentQuery(Guid EnrollmentId) : IRequest<Result<GetByIdEnrollmentQueryDto>>;
