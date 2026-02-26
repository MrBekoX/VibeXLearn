using Platform.Domain.Common;
using Platform.Domain.Enums;
using Platform.Domain.Events;

namespace Platform.Domain.Entities;

/// <summary>
/// Submission with review workflow.
/// </summary>
public class Submission : AuditableEntity, IAggregateRoot
{
    // Private setters for encapsulation
    public string           RepoUrl    { get; private set; } = default!;
    public string?          CommitSha  { get; private set; }
    public string?          Branch     { get; private set; }
    public string?          PrUrl      { get; private set; }
    public SubmissionStatus Status     { get; private set; } = SubmissionStatus.Pending;
    public string?          ReviewNote { get; private set; }
    public Guid             StudentId  { get; private set; }
    public Guid             LessonId   { get; private set; }

    // Navigation properties
    public AppUser          Student    { get; private set; } = default!;
    public Lesson           Lesson     { get; private set; } = default!;

    // Private constructor for EF Core
    private Submission() { }

    /// <summary>
    /// Factory method to create a new submission.
    /// </summary>
    public static Submission Create(
        Guid studentId,
        Guid lessonId,
        string repoUrl,
        string? commitSha = null,
        string? branch = null,
        string? prUrl = null)
    {
        Guard.Against.EmptyGuid(studentId, nameof(studentId));
        Guard.Against.EmptyGuid(lessonId, nameof(lessonId));
        Guard.Against.NullOrWhiteSpace(repoUrl, nameof(repoUrl));

        var submission = new Submission
        {
            StudentId = studentId,
            LessonId = lessonId,
            RepoUrl = repoUrl.Trim(),
            CommitSha = commitSha?.Trim(),
            Branch = branch?.Trim() ?? "main",
            PrUrl = prUrl?.Trim(),
            Status = SubmissionStatus.Pending
        };

        submission.AddDomainEvent(new SubmissionCreatedEvent(submission.Id, studentId, lessonId));
        return submission;
    }

    /// <summary>
    /// Update submission with new commit.
    /// </summary>
    public void UpdateCommit(string commitSha, string? branch = null)
    {
        if (Status == SubmissionStatus.Accepted)
            throw new DomainException("SUBMISSION_UPDATE_ACCEPTED",
                "Cannot update an accepted submission.");

        Guard.Against.NullOrWhiteSpace(commitSha, nameof(commitSha));

        CommitSha = commitSha.Trim();
        if (branch is not null)
            Branch = branch.Trim();

        // Reset to pending if was rejected
        if (Status == SubmissionStatus.Rejected)
            Status = SubmissionStatus.Pending;

        MarkAsUpdated();
    }

    /// <summary>
    /// Update PR URL.
    /// </summary>
    public void UpdatePrUrl(string prUrl)
    {
        PrUrl = prUrl?.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Start validation process.
    /// </summary>
    public void StartValidation()
    {
        if (Status != SubmissionStatus.Pending)
            throw new DomainException("SUBMISSION_VALIDATION_INVALID_STATUS",
                "Only pending submissions can be validated.");

        Status = SubmissionStatus.Validating;
        MarkAsUpdated();
    }

    /// <summary>
    /// Accept the submission.
    /// </summary>
    public void Accept(string? reviewNote = null)
    {
        if (Status != SubmissionStatus.Pending && Status != SubmissionStatus.Validating)
            throw new DomainException("SUBMISSION_ACCEPT_INVALID_STATUS",
                "Only pending or validating submissions can be accepted.");

        Status = SubmissionStatus.Accepted;
        ReviewNote = reviewNote?.Trim();
        MarkAsUpdated();

        AddDomainEvent(new SubmissionAcceptedEvent(Id, StudentId));
    }

    /// <summary>
    /// Reject the submission.
    /// </summary>
    public void Reject(string? reviewNote = null)
    {
        if (Status != SubmissionStatus.Pending && Status != SubmissionStatus.Validating)
            throw new DomainException("SUBMISSION_REJECT_INVALID_STATUS",
                "Only pending or validating submissions can be rejected.");

        Status = SubmissionStatus.Rejected;
        ReviewNote = reviewNote?.Trim();
        MarkAsUpdated();

        AddDomainEvent(new SubmissionRejectedEvent(Id, StudentId, reviewNote));
    }

    /// <summary>
    /// Check if submission can be updated.
    /// </summary>
    public bool CanUpdate => Status != SubmissionStatus.Accepted;

    /// <summary>
    /// Check if submission is pending review.
    /// </summary>
    public bool IsPendingReview => Status == SubmissionStatus.Pending;

    /// <summary>
    /// Check if submission is accepted.
    /// </summary>
    public bool IsAccepted => Status == SubmissionStatus.Accepted;
}
