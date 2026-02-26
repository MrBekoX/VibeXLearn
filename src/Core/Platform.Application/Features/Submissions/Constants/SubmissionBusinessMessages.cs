namespace Platform.Application.Features.Submissions.Constants;

/// <summary>
/// Submission business rule messages.
/// </summary>
public static class SubmissionBusinessMessages
{
    // Not Found
    public const string NotFound = "Submission not found.";
    public const string NotFoundById = "Submission not found with the specified ID.";

    // Status Errors
    public const string AlreadyAccepted = "Submission is already accepted.";
    public const string AlreadyRejected = "Submission is already rejected.";
    public const string OnlyPendingCanBeReviewed = "Only pending or validating submissions can be reviewed.";
    public const string CannotModifyReviewed = "Cannot modify a submission that has been reviewed.";

    // Validation Errors
    public const string StudentAlreadySubmitted = "Student has already submitted for this lesson.";
    public const string RepoUrlInvalid = "Repository URL must be a valid GitHub URL.";
    public const string CommitShaInvalid = "Commit SHA must be a valid 40-character SHA-1 hash.";
    public const string BranchNameInvalid = "Branch name contains invalid characters.";

    // Processing Errors
    public const string ValidationInProgress = "Submission validation is in progress.";
    public const string ValidationFailed = "Submission validation failed.";

    // Success Messages
    public const string CreatedSuccessfully = "Submission created successfully.";
    public const string AcceptedSuccessfully = "Submission accepted successfully.";
    public const string RejectedSuccessfully = "Submission rejected successfully.";
    public const string ValidatingSuccessfully = "Submission validation started.";
}
