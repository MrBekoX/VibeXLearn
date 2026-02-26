using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Submissions.Commands.ReviewSubmission;

/// <summary>
/// Command to review (accept/reject) a submission.
/// </summary>
public sealed record ReviewSubmissionCommand(
    Guid SubmissionId,
    bool Accept,
    string? ReviewNote = null) : IRequest<Result>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["submissions:*"];
}
