using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Auth.Rules;

/// <summary>
/// Auth business rules interface.
/// </summary>
public interface IAuthBusinessRules
{
    /// <summary>
    /// Rule: Email must not be already registered.
    /// </summary>
    IBusinessRule EmailMustBeUnique(string email);

    /// <summary>
    /// Rule: User must exist (not deleted).
    /// </summary>
    IBusinessRule UserMustExist(Guid userId);
}
