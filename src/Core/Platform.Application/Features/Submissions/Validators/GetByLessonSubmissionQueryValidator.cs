using FluentValidation;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.Queries.GetByLessonSubmission;

namespace Platform.Application.Features.Submissions.Validators;

/// <summary>
/// Validator for GetByLessonSubmissionQuery.
/// </summary>
public sealed class GetByLessonSubmissionQueryValidator : AbstractValidator<GetByLessonSubmissionQuery>
{
    public GetByLessonSubmissionQueryValidator()
    {
        RuleFor(x => x.LessonId)
            .NotEmpty().WithMessage(SubmissionValidationMessages.LessonIdEmpty);
    }
}
