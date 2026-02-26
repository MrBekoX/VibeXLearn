using FluentValidation;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.Queries.GetByStudentSubmission;

namespace Platform.Application.Features.Submissions.Validators;

/// <summary>
/// Validator for GetByStudentSubmissionQuery.
/// </summary>
public sealed class GetByStudentSubmissionQueryValidator : AbstractValidator<GetByStudentSubmissionQuery>
{
    public GetByStudentSubmissionQueryValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage(SubmissionValidationMessages.StudentIdEmpty);
    }
}
