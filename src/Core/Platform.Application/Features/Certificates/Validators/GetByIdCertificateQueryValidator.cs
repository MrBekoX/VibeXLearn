using FluentValidation;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.Queries.GetByIdCertificate;

namespace Platform.Application.Features.Certificates.Validators;

/// <summary>
/// Validator for GetByIdCertificateQuery.
/// </summary>
public sealed class GetByIdCertificateQueryValidator : AbstractValidator<GetByIdCertificateQuery>
{
    public GetByIdCertificateQueryValidator()
    {
        RuleFor(x => x.CertificateId)
            .NotEmpty().WithMessage(CertificateValidationMessages.CertificateIdEmpty);
    }
}
