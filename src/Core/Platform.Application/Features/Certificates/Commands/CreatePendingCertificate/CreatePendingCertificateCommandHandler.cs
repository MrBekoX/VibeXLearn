using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Certificates.Constants;
using Platform.Application.Features.Certificates.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Certificates.Commands.CreatePendingCertificate;

/// <summary>
/// Handler for CreatePendingCertificateCommand.
/// </summary>
public sealed class CreatePendingCertificateCommandHandler(
    IWriteRepository<Certificate> writeRepo,
    ICertificateBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CreatePendingCertificateCommandHandler> logger) : IRequestHandler<CreatePendingCertificateCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePendingCertificateCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.UserMustNotHaveCertificateForCourse(request.UserId, request.CourseId));
        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        var certificate = Certificate.CreatePending(request.UserId, request.CourseId);
        await writeRepo.AddAsync(certificate, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Pending certificate created: {CertificateId} for User: {UserId}, Course: {CourseId}",
            certificate.Id, request.UserId, request.CourseId);

        return Result.Success(certificate.Id);
    }
}
