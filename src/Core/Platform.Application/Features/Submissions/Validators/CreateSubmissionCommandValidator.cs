using System.Text.RegularExpressions;
using FluentValidation;
using Platform.Application.Features.Submissions.Commands.CreateSubmission;
using Platform.Application.Features.Submissions.Constants;

namespace Platform.Application.Features.Submissions.Validators;

/// <summary>
/// Validator for CreateSubmissionCommand.
/// </summary>
public sealed class CreateSubmissionCommandValidator : AbstractValidator<CreateSubmissionCommand>
{
    private static readonly Regex GitHubUrlPattern = new(@"^https?://(www\.)?github\.com/[\w-]+/[\w.-]+/?$", RegexOptions.Compiled);
    private static readonly Regex ShaPattern = new(@"^[a-fA-F0-9]{40}$", RegexOptions.Compiled);
    private static readonly Regex BranchPattern = new(@"^[a-zA-Z0-9_\-./]+$", RegexOptions.Compiled);

    public CreateSubmissionCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage(SubmissionValidationMessages.StudentIdEmpty);

        RuleFor(x => x.LessonId)
            .NotEmpty().WithMessage(SubmissionValidationMessages.LessonIdEmpty);

        RuleFor(x => x.RepoUrl)
            .NotEmpty().WithMessage(SubmissionValidationMessages.RepoUrlRequired)
            .MaximumLength(500).WithMessage(SubmissionValidationMessages.RepoUrlMaxLength)
            .Must(BeAValidGitHubUrl).WithMessage(SubmissionValidationMessages.RepoUrlInvalidFormat);

        RuleFor(x => x.CommitSha)
            .MaximumLength(40).WithMessage(SubmissionValidationMessages.CommitShaMaxLength)
            .Must(BeAValidSha).WithMessage(SubmissionValidationMessages.CommitShaInvalidFormat)
            .When(x => x.CommitSha is not null);

        RuleFor(x => x.Branch)
            .MaximumLength(200).WithMessage(SubmissionValidationMessages.BranchMaxLength)
            .Must(BeAValidBranchName).WithMessage(SubmissionValidationMessages.BranchInvalidCharacters)
            .When(x => x.Branch is not null);
    }

    private static bool BeAValidGitHubUrl(string url)
    {
        return GitHubUrlPattern.IsMatch(url);
    }

    private static bool BeAValidSha(string sha)
    {
        return ShaPattern.IsMatch(sha);
    }

    private static bool BeAValidBranchName(string branch)
    {
        return BranchPattern.IsMatch(branch);
    }
}
