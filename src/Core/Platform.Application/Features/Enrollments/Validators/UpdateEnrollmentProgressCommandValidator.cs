using FluentValidation;
using Platform.Application.Features.Enrollments.Commands.UpdateEnrollmentProgress;
using Platform.Application.Features.Enrollments.Constants;

namespace Platform.Application.Features.Enrollments.Validators;

public sealed class UpdateEnrollmentProgressCommandValidator : AbstractValidator<UpdateEnrollmentProgressCommand>
{
    public UpdateEnrollmentProgressCommandValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage(EnrollmentValidationMessages.EnrollmentIdRequired);

        RuleFor(x => x.Progress)
            .InclusiveBetween(0, 100).WithMessage(EnrollmentValidationMessages.ProgressRange);
    }
}
