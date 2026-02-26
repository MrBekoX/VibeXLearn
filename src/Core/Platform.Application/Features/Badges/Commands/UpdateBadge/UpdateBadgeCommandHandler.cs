using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Badges.Constants;
using Platform.Application.Features.Badges.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Badges.Commands.UpdateBadge;

/// <summary>
/// Handler for UpdateBadgeCommand.
/// </summary>
public sealed class UpdateBadgeCommandHandler(
    IReadRepository<Badge> readRepo,
    IWriteRepository<Badge> writeRepo,
    IBadgeBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<UpdateBadgeCommandHandler> logger) : IRequestHandler<UpdateBadgeCommand, Result>
{
    public async Task<Result> Handle(UpdateBadgeCommand request, CancellationToken ct)
    {
        var ruleResult = await ruleEngine.RunAsync(ct, rules.BadgeMustExist(request.BadgeId));
        if (ruleResult.IsFailure)
            return ruleResult;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var nameResult = await ruleEngine.RunAsync(ct,
                rules.NameMustBeUnique(request.Name, request.BadgeId));
            if (nameResult.IsFailure)
                return nameResult;
        }

        var badge = await readRepo.GetByIdAsync(request.BadgeId, ct, tracking: true);
        if (badge is null)
            return Result.Fail("BADGE_NOT_FOUND", BadgeBusinessMessages.NotFound);

        if (request.Name is not null || request.Description is not null)
        {
            badge.UpdateDetails(
                request.Name ?? badge.Name,
                request.Description ?? badge.Description);
        }

        if (request.IconUrl is not null)
            badge.UpdateIcon(request.IconUrl);

        await writeRepo.UpdateAsync(badge, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Badge updated: {BadgeId}", request.BadgeId);

        return Result.Success();
    }
}
