using FluentValidation;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.Queries.GetByUserCertificate;

namespace Platform.Application.Features.Certificates.Validators;

/// <summary>
/// Validator for GetByUserCertificateQuery.
/// </summary>
public sealed class GetByUserCertificateQueryValidator : AbstractValidator<GetByUserCertificateQuery>
{
    public GetByUserCertificateQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(CertificateValidationMessages.UserIdEmpty);
    }
}
