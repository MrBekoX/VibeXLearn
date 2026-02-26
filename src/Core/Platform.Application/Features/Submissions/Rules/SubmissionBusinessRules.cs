using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Submissions.Constants;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Submissions.Rules;

/// <summary>
/// Submission business rules implementation.
/// </summary>
public sealed class SubmissionBusinessRules(IReadRepository<Submission> repo) : ISubmissionBusinessRules
{
    public IBusinessRule SubmissionMustExist(Guid submissionId)
        => new BusinessRule(
            "SUBMISSION_NOT_FOUND",
            SubmissionBusinessMessages.NotFound,
            async ct => await repo.AnyAsync(s => s.Id == submissionId, ct)
                ? Result.Success()
                : Result.Fail(SubmissionBusinessMessages.NotFound));

    public IBusinessRule SubmissionMustBePending(Guid submissionId)
        => new BusinessRule(
            "SUBMISSION_NOT_PENDING",
            SubmissionBusinessMessages.OnlyPendingCanBeReviewed,
            async ct =>
            {
                var submission = await repo.GetByIdAsync(submissionId, ct);
                return submission?.Status == SubmissionStatus.Pending || submission?.Status == SubmissionStatus.Validating
                    ? Result.Success()
                    : Result.Fail(SubmissionBusinessMessages.OnlyPendingCanBeReviewed);
            });

    public IBusinessRule StudentMustNotHaveSubmittedForLesson(Guid studentId, Guid lessonId)
        => new BusinessRule(
            "SUBMISSION_EXISTS",
            SubmissionBusinessMessages.StudentAlreadySubmitted,
            async ct =>
            {
                var exists = await repo.AnyAsync(
                    s => s.StudentId == studentId && s.LessonId == lessonId, ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail(SubmissionBusinessMessages.StudentAlreadySubmitted);
            });
}

/// <summary>
/// Simple business rule implementation.
/// </summary>
file sealed class BusinessRule(string code, string message, Func<CancellationToken, Task<Result>> checkFunc)
    : IBusinessRule
{
    public string Code => code;
    public string Message => message;
    public Task<Result> CheckAsync(CancellationToken ct) => checkFunc(ct);
}
