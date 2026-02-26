using FluentValidation;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.Queries.GetByUserEnrollment;

namespace Platform.Application.Features.Enrollments.Validators;

public sealed class GetByUserEnrollmentQueryValidator : AbstractValidator<GetByUserEnrollmentQuery>
{
    public GetByUserEnrollmentQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(EnrollmentValidationMessages.UserIdRequired);
    }
}
