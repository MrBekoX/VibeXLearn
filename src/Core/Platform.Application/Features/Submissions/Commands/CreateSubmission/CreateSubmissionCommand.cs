using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Submissions.Commands.CreateSubmission;

/// <summary>
/// Command to create a new submission.
/// </summary>
public sealed record CreateSubmissionCommand(
    Guid StudentId,
    Guid LessonId,
    string RepoUrl,
    string? CommitSha = null,
    string? Branch = null) : IRequest<Result<Guid>>, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheInvalidationPatterns => ["submissions:*"];
}
