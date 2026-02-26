using FluentValidation;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.Queries.GetByIdSubmission;

namespace Platform.Application.Features.Submissions.Validators;

/// <summary>
/// Validator for GetByIdSubmissionQuery.
/// </summary>
public sealed class GetByIdSubmissionQueryValidator : AbstractValidator<GetByIdSubmissionQuery>
{
    public GetByIdSubmissionQueryValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty().WithMessage(SubmissionValidationMessages.SubmissionIdEmpty);
    }
}
