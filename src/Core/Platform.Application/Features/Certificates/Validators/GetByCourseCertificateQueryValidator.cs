using FluentValidation;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.Queries.GetByCourseCertificate;

namespace Platform.Application.Features.Certificates.Validators;

/// <summary>
/// Validator for GetByCourseCertificateQuery.
/// </summary>
public sealed class GetByCourseCertificateQueryValidator : AbstractValidator<GetByCourseCertificateQuery>
{
    public GetByCourseCertificateQueryValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage(CertificateValidationMessages.CourseIdEmpty);
    }
}
