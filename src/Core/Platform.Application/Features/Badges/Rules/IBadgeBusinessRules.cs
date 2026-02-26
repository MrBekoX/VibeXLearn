using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Badges.Rules;

/// <summary>
/// Badge business rules interface.
/// </summary>
public interface IBadgeBusinessRules
{
    /// <summary>
    /// Rule: Badge must exist in the system.
    /// </summary>
    IBusinessRule BadgeMustExist(Guid badgeId);

    /// <summary>
    /// Rule: Badge name must be unique.
    /// </summary>
    IBusinessRule NameMustBeUnique(string name, Guid? excludeBadgeId = null);
}
