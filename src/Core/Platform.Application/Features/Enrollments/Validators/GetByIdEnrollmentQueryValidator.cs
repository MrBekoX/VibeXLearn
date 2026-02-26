using FluentValidation;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.Queries.GetByIdEnrollment;

namespace Platform.Application.Features.Enrollments.Validators;

public sealed class GetByIdEnrollmentQueryValidator : AbstractValidator<GetByIdEnrollmentQuery>
{
    public GetByIdEnrollmentQueryValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage(EnrollmentValidationMessages.EnrollmentIdRequired);
    }
}
