using FluentValidation;
using Platform.Application.Features.Enrollments.Constants;
using Platform.Application.Features.Enrollments.Queries.GetByCourseEnrollment;

namespace Platform.Application.Features.Enrollments.Validators;

public sealed class GetByCourseEnrollmentQueryValidator : AbstractValidator<GetByCourseEnrollmentQuery>
{
    public GetByCourseEnrollmentQueryValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(EnrollmentValidationMessages.CourseIdRequired);
    }
}
