using FluentValidation;
using Platform.Application.Features.Certificates.Commands.MarkCertificateAsIssued;
using Platform.Application.Features.Certificates.Constants;

namespace Platform.Application.Features.Certificates.Validators;

/// <summary>
/// Validator for MarkCertificateAsIssuedCommand.
/// </summary>
public sealed class MarkCertificateAsIssuedCommandValidator : AbstractValidator<MarkCertificateAsIssuedCommand>
{
    public MarkCertificateAsIssuedCommandValidator()
    {
        RuleFor(x => x.CertificateId)
            .NotEmpty().WithMessage(CertificateValidationMessages.CertificateIdEmpty);

        RuleFor(x => x.SertifierCertId)
            .NotEmpty().WithMessage(CertificateValidationMessages.SertifierCertIdRequired)
            .MaximumLength(200).WithMessage(CertificateValidationMessages.SertifierCertIdMaxLength);

        RuleFor(x => x.PublicUrl)
            .NotEmpty().WithMessage(CertificateValidationMessages.PublicUrlRequired)
            .MaximumLength(500).WithMessage(CertificateValidationMessages.PublicUrlMaxLength)
            .Must(BeAValidUrl).WithMessage(CertificateValidationMessages.PublicUrlInvalidFormat);
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
