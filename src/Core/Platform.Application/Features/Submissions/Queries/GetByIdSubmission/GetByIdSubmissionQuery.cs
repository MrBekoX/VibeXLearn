using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Submissions.DTOs;

namespace Platform.Application.Features.Submissions.Queries.GetByIdSubmission;

/// <summary>
/// Query to get a submission by ID.
/// </summary>
public sealed record GetByIdSubmissionQuery(Guid SubmissionId) : IRequest<Result<GetByIdSubmissionQueryDto>>;
