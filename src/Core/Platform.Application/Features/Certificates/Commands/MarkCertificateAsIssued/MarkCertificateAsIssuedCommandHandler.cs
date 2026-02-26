using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Certificates.Commands.MarkCertificateAsIssued;

/// <summary>
/// Handler for MarkCertificateAsIssuedCommand.
/// </summary>
public sealed class MarkCertificateAsIssuedCommandHandler(
    IReadRepository<Certificate> readRepo,
    IWriteRepository<Certificate> writeRepo,
    ICertificateBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<MarkCertificateAsIssuedCommandHandler> logger) : IRequestHandler<MarkCertificateAsIssuedCommand, Result>
{
    public async Task<Result> Handle(MarkCertificateAsIssuedCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CertificateMustExist(request.CertificateId),
            rules.CertificateMustBePending(request.CertificateId));
        if (ruleResult.IsFailure)
            return ruleResult;

        var certificate = await readRepo.GetByIdAsync(request.CertificateId, ct, tracking: true);
        if (certificate is null)
            return Result.Fail("CERTIFICATE_NOT_FOUND", CertificateBusinessMessages.NotFound);

        certificate.MarkAsIssued(request.SertifierCertId, request.PublicUrl);
        await writeRepo.UpdateAsync(certificate, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Certificate issued: {CertificateId}", request.CertificateId);

        return Result.Success();
    }
}
