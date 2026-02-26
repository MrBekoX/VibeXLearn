using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Badges.Constants;
using Platform.Application.Features.Badges.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Badges.Commands.DeleteBadge;

/// <summary>
/// Handler for DeleteBadgeCommand.
/// </summary>
public sealed class DeleteBadgeCommandHandler(
    IReadRepository<Badge> readRepo,
    IWriteRepository<Badge> writeRepo,
    IBadgeBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<DeleteBadgeCommandHandler> logger) : IRequestHandler<DeleteBadgeCommand, Result>
{
    public async Task<Result> Handle(DeleteBadgeCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct, rules.BadgeMustExist(request.BadgeId));
        if (ruleResult.IsFailure)
            return ruleResult;

        // Get badge with tracking
        var badge = await readRepo.GetByIdAsync(request.BadgeId, ct, tracking: true);
        if (badge is null)
            return Result.Fail("BADGE_NOT_FOUND", BadgeBusinessMessages.NotFoundById);

        // Soft delete
        await writeRepo.SoftDeleteAsync(badge, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Badge deleted: {BadgeId}", request.BadgeId);

        return Result.Success();
    }
}
