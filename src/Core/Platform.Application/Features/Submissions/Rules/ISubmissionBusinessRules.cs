using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Submissions.Rules;

/// <summary>
/// Submission business rules interface.
/// </summary>
public interface ISubmissionBusinessRules
{
    /// <summary>
    /// Rule: Submission must exist in the system.
    /// </summary>
    IBusinessRule SubmissionMustExist(Guid submissionId);

    /// <summary>
    /// Rule: Submission must be in Pending or Validating status for review.
    /// </summary>
    IBusinessRule SubmissionMustBePending(Guid submissionId);

    /// <summary>
    /// Rule: Student must not have already submitted for the lesson.
    /// </summary>
    IBusinessRule StudentMustNotHaveSubmittedForLesson(Guid studentId, Guid lessonId);
}
