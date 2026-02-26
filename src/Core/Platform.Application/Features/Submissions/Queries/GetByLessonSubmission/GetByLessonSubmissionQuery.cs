using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Submissions.DTOs;

namespace Platform.Application.Features.Submissions.Queries.GetByLessonSubmission;

/// <summary>
/// Query to get all submissions for a lesson.
/// </summary>
public sealed record GetByLessonSubmissionQuery(Guid LessonId) : IRequest<Result<IList<GetAllSubmissionsQueryDto>>>;
