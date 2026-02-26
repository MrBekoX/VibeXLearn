using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Submissions.Commands.CreateSubmission;

/// <summary>
/// Handler for CreateSubmissionCommand.
/// </summary>
public sealed class CreateSubmissionCommandHandler(
    IWriteRepository<Submission> writeRepo,
    ISubmissionBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CreateSubmissionCommandHandler> logger) : IRequestHandler<CreateSubmissionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSubmissionCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.StudentMustNotHaveSubmittedForLesson(request.StudentId, request.LessonId));
        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        var submission = Submission.Create(request.StudentId, request.LessonId, request.RepoUrl, request.CommitSha, request.Branch);
        await writeRepo.AddAsync(submission, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Submission created: {SubmissionId} for Student: {StudentId}, Lesson: {LessonId}",
            submission.Id, request.StudentId, request.LessonId);

        return Result.Success(submission.Id);
    }
}
