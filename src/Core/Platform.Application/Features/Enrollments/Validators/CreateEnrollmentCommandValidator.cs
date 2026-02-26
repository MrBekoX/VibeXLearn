using FluentValidation;
using Platform.Application.Features.Enrollments.Commands.CreateEnrollment;
using Platform.Application.Features.Enrollments.Constants;

namespace Platform.Application.Features.Enrollments.Validators;

public sealed class CreateEnrollmentCommandValidator : AbstractValidator<CreateEnrollmentCommand>
{
    public CreateEnrollmentCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(EnrollmentValidationMessages.UserIdRequired);

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(EnrollmentValidationMessages.CourseIdRequired);
    }
}
