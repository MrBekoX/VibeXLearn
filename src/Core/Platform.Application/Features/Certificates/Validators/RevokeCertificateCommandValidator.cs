using FluentValidation;
using Platform.Application.Features.Certificates.Commands.RevokeCertificate;
using Platform.Application.Features.Certificates.Constants;

namespace Platform.Application.Features.Certificates.Validators;

/// <summary>
/// Validator for RevokeCertificateCommand.
/// </summary>
public sealed class RevokeCertificateCommandValidator : AbstractValidator<RevokeCertificateCommand>
{
    public RevokeCertificateCommandValidator()
    {
        RuleFor(x => x.CertificateId)
            .NotEmpty().WithMessage(CertificateValidationMessages.CertificateIdEmpty);
    }
}
