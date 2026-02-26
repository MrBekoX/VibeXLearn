using FluentValidation;
using Platform.Application.Features.Submissions.Commands.ReviewSubmission;
using Platform.Application.Features.Submissions.Constants;

namespace Platform.Application.Features.Submissions.Validators;

/// <summary>
/// Validator for ReviewSubmissionCommand.
/// </summary>
public sealed class ReviewSubmissionCommandValidator : AbstractValidator<ReviewSubmissionCommand>
{
    public ReviewSubmissionCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty().WithMessage(SubmissionValidationMessages.SubmissionIdEmpty);

        RuleFor(x => x.ReviewNote)
            .MaximumLength(2000).WithMessage(SubmissionValidationMessages.ReviewNoteMaxLength)
            .When(x => x.ReviewNote is not null);
    }
}
