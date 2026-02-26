using FluentValidation;
using Platform.Application.Features.Enrollments.Commands.CompleteEnrollment;
using Platform.Application.Features.Enrollments.Constants;

namespace Platform.Application.Features.Enrollments.Validators;

public sealed class CompleteEnrollmentCommandValidator : AbstractValidator<CompleteEnrollmentCommand>
{
    public CompleteEnrollmentCommandValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage(EnrollmentValidationMessages.EnrollmentIdRequired);
    }
}
