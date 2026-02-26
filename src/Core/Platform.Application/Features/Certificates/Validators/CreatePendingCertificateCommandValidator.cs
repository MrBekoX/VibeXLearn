using FluentValidation;
using Platform.Application.Features.Certificates.Commands.CreatePendingCertificate;
using Platform.Application.Features.Certificates.Constants;

namespace Platform.Application.Features.Certificates.Validators;

/// <summary>
/// Validator for CreatePendingCertificateCommand.
/// </summary>
public sealed class CreatePendingCertificateCommandValidator : AbstractValidator<CreatePendingCertificateCommand>
{
    public CreatePendingCertificateCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(CertificateValidationMessages.UserIdEmpty);

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(CertificateValidationMessages.CourseIdEmpty);
    }
}
