using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.DTOs;

namespace Platform.Application.Features.Submissions.Queries.GetByStudentSubmission;

/// <summary>
/// Query to get all submissions by a student.
/// </summary>
public sealed record GetByStudentSubmissionQuery(Guid StudentId)
    : IRequest<Result<IList<GetByStudentSubmissionQueryDto>>>, ICacheableQuery
{
    public string CacheKey => SubmissionCacheKeys.ByStudent(StudentId);
    public TimeSpan L2Duration => TimeSpan.Zero;
    public bool BypassCache => false;
}
