using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Constants;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handler for LoginCommand.
/// </summary>
public sealed class LoginCommandHandler(
    IAuthService authService,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, ct);

        if (result.IsFailure)
        {
            logger.LogWarning("{Event} | Email:{Email}",
                SecurityAuditEvents.LoginFailed, request.Email);
            return Result.Fail<LoginResult>(result.Error);
        }

        var (accessToken, expiresAt, refreshToken) = result.Value;

        logger.LogInformation("{Event} | Email:{Email}",
            SecurityAuditEvents.LoginSuccess, request.Email);

        return Result.Success(new LoginResult(accessToken, expiresAt, refreshToken));
    }
}
