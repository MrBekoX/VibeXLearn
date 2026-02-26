using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Certificates.Constants;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Certificates.Rules;

/// <summary>
/// Certificate business rules implementation.
/// </summary>
public sealed class CertificateBusinessRules(IReadRepository<Certificate> repo) : ICertificateBusinessRules
{
    public IBusinessRule CertificateMustExist(Guid certificateId)
        => new BusinessRule(
            "CERTIFICATE_NOT_FOUND",
            CertificateBusinessMessages.NotFound,
            async ct => await repo.AnyAsync(c => c.Id == certificateId, ct)
                ? Result.Success()
                : Result.Fail(CertificateBusinessMessages.NotFound));

    public IBusinessRule CertificateMustBePending(Guid certificateId)
        => new BusinessRule(
            "CERTIFICATE_NOT_PENDING",
            CertificateBusinessMessages.NotPending,
            async ct =>
            {
                var certificate = await repo.GetByIdAsync(certificateId, ct);
                return certificate?.Status == CertificateStatus.Pending
                    ? Result.Success()
                    : Result.Fail(CertificateBusinessMessages.NotPending);
            });

    public IBusinessRule CertificateMustNotBeRevoked(Guid certificateId)
        => new BusinessRule(
            "CERTIFICATE_REVOKED",
            CertificateBusinessMessages.AlreadyRevoked,
            async ct =>
            {
                var certificate = await repo.GetByIdAsync(certificateId, ct);
                return certificate?.Status != CertificateStatus.Revoked
                    ? Result.Success()
                    : Result.Fail(CertificateBusinessMessages.AlreadyRevoked);
            });

    public IBusinessRule UserMustNotHaveCertificateForCourse(Guid userId, Guid courseId)
        => new BusinessRule(
            "CERTIFICATE_ALREADY_EXISTS",
            CertificateBusinessMessages.UserAlreadyHasCertificate,
            async ct =>
            {
                var exists = await repo.AnyAsync(
                    c => c.UserId == userId && c.CourseId == courseId && c.Status != CertificateStatus.Revoked,
                    ct);
                return !exists
                    ? Result.Success()
                    : Result.Fail(CertificateBusinessMessages.UserAlreadyHasCertificate);
            });
}

/// <summary>
/// Simple business rule implementation.
/// </summary>
file sealed class BusinessRule(string code, string message, Func<CancellationToken, Task<Result>> checkFunc)
    : IBusinessRule
{
    public string Code => code;
    public string Message => message;
    public Task<Result> CheckAsync(CancellationToken ct) => checkFunc(ct);
}
