namespace Platform.Application.Features.Submissions.Constants;

/// <summary>
/// Submission validation messages.
/// </summary>
public static class SubmissionValidationMessages
{
    // ID
    public const string SubmissionIdRequired = "Submission ID is required.";
    public const string SubmissionIdEmpty = "Submission ID cannot be empty.";

    // Student ID
    public const string StudentIdRequired = "Student ID is required.";
    public const string StudentIdEmpty = "Student ID cannot be empty.";

    // Lesson ID
    public const string LessonIdRequired = "Lesson ID is required.";
    public const string LessonIdEmpty = "Lesson ID cannot be empty.";

    // Repository URL
    public const string RepoUrlRequired = "Repository URL is required.";
    public const string RepoUrlMaxLength = "Repository URL cannot exceed 500 characters.";
    public const string RepoUrlInvalidFormat = "Repository URL must be a valid GitHub URL.";

    // Commit SHA
    public const string CommitShaMaxLength = "Commit SHA cannot exceed 40 characters.";
    public const string CommitShaInvalidFormat = "Commit SHA must be a valid 40-character hexadecimal string.";

    // Branch
    public const string BranchMaxLength = "Branch name cannot exceed 200 characters.";
    public const string BranchInvalidCharacters = "Branch name contains invalid characters.";

    // Review Note
    public const string ReviewNoteMaxLength = "Review note cannot exceed 2000 characters.";
}
