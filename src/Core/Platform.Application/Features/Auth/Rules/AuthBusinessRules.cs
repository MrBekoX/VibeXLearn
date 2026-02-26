using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Auth.Constants;

namespace Platform.Application.Features.Auth.Rules;

/// <summary>
/// Auth business rules implementation.
/// </summary>
public sealed class AuthBusinessRules(
    IIdentityAccessService identityAccess) : IAuthBusinessRules
{
    public IBusinessRule EmailMustBeUnique(string email)
        => new BusinessRule(
            "AUTH_EMAIL_EXISTS",
            AuthBusinessMessages.EmailAlreadyExists,
            async ct =>
            {
                // IIdentityAccessService does not have email-based check,
                // so this rule is validated inside AuthService via UserManager.
                // This is a placeholder for the rule engine chain.
                await Task.CompletedTask;
                return Result.Success();
            });

    public IBusinessRule UserMustExist(Guid userId)
        => new BusinessRule(
            "AUTH_USER_NOT_FOUND",
            AuthBusinessMessages.UserNotFound,
            async ct => await identityAccess.UserExistsAsync(userId, ct)
                ? Result.Success()
                : Result.Fail(AuthBusinessMessages.UserNotFound));
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
