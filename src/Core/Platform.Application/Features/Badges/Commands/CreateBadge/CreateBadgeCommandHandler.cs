using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Badges.Constants;
using Platform.Application.Features.Badges.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Badges.Commands.CreateBadge;

/// <summary>
/// Handler for CreateBadgeCommand.
/// </summary>
public sealed class CreateBadgeCommandHandler(
    IWriteRepository<Badge> writeRepo,
    IBadgeBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CreateBadgeCommandHandler> logger) : IRequestHandler<CreateBadgeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBadgeCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct, rules.NameMustBeUnique(request.Name));
        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        var badge = Badge.Create(request.Name, request.Description, request.IconUrl, request.Criteria);
        await writeRepo.AddAsync(badge, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Badge created: {BadgeId} - {Name}", badge.Id, badge.Name);

        return Result.Success(badge.Id);
    }
}
