using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Submissions.Commands.ReviewSubmission;

/// <summary>
/// Handler for ReviewSubmissionCommand.
/// </summary>
public sealed class ReviewSubmissionCommandHandler(
    IReadRepository<Submission> readRepo,
    IWriteRepository<Submission> writeRepo,
    ISubmissionBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<ReviewSubmissionCommandHandler> logger) : IRequestHandler<ReviewSubmissionCommand, Result>
{
    public async Task<Result> Handle(ReviewSubmissionCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.SubmissionMustExist(request.SubmissionId),
            rules.SubmissionMustBePending(request.SubmissionId));
        if (ruleResult.IsFailure)
            return ruleResult;

        var submission = await readRepo.GetByIdAsync(request.SubmissionId, ct, tracking: true);
        if (submission is null)
            return Result.Fail("SUBMISSION_NOT_FOUND", SubmissionBusinessMessages.NotFound);

        if (request.Accept)
        {
            submission.Accept(request.ReviewNote);
            logger.LogInformation("Submission accepted: {SubmissionId}", request.SubmissionId);
        }
        else
        {
            submission.Reject(request.ReviewNote);
            logger.LogInformation("Submission rejected: {SubmissionId}", request.SubmissionId);
        }

        await writeRepo.UpdateAsync(submission, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
