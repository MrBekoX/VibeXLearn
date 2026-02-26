using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Certificates.Commands.RevokeCertificate;

/// <summary>
/// Handler for RevokeCertificateCommand.
/// </summary>
public sealed class RevokeCertificateCommandHandler(
    IReadRepository<Certificate> readRepo,
    IWriteRepository<Certificate> writeRepo,
    ICertificateBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<RevokeCertificateCommandHandler> logger) : IRequestHandler<RevokeCertificateCommand, Result>
{
    public async Task<Result> Handle(RevokeCertificateCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CertificateMustExist(request.CertificateId),
            rules.CertificateMustNotBeRevoked(request.CertificateId));
        if (ruleResult.IsFailure)
            return ruleResult;

        var certificate = await readRepo.GetByIdAsync(request.CertificateId, ct, tracking: true);
        if (certificate is null)
            return Result.Fail("CERTIFICATE_NOT_FOUND", CertificateBusinessMessages.NotFound);

        certificate.Revoke();
        await writeRepo.UpdateAsync(certificate, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Certificate revoked: {CertificateId}", request.CertificateId);

        return Result.Success();
    }
}
