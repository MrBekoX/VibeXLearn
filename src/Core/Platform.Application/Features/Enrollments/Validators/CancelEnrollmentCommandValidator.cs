using FluentValidation;
using Platform.Application.Features.Enrollments.Commands.CancelEnrollment;
using Platform.Application.Features.Enrollments.Constants;

namespace Platform.Application.Features.Enrollments.Validators;

public sealed class CancelEnrollmentCommandValidator : AbstractValidator<CancelEnrollmentCommand>
{
    public CancelEnrollmentCommandValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage(EnrollmentValidationMessages.EnrollmentIdRequired);
    }
}
