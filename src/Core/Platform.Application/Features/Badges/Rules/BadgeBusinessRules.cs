using Microsoft.EntityFrameworkCore;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Badges.Constants;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Badges.Rules;

/// <summary>
/// Badge business rules implementation.
/// </summary>
public sealed class BadgeBusinessRules(IReadRepository<Badge> repo) : IBadgeBusinessRules
{
    public IBusinessRule BadgeMustExist(Guid badgeId)
        => new BusinessRule(
            "BADGE_NOT_FOUND",
            BadgeBusinessMessages.NotFound,
            async ct => await repo.AnyAsync(b => b.Id == badgeId, ct)
                ? Result.Success()
                : Result.Fail(BadgeBusinessMessages.NotFound));

    public IBusinessRule NameMustBeUnique(string name, Guid? excludeBadgeId = null)
        => new BusinessRule(
            "BADGE_NAME_EXISTS",
            BadgeBusinessMessages.NameAlreadyExists,
            async ct =>
            {
                var query = repo.GetQuery().Where(b => b.Name.ToLower() == name.ToLower());
                if (excludeBadgeId.HasValue)
                    query = query.Where(b => b.Id != excludeBadgeId.Value);
                return !await query.AnyAsync()
                    ? Result.Success()
                    : Result.Fail(BadgeBusinessMessages.NameAlreadyExists);
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
