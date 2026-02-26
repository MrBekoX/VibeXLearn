using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Constants;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Handler for LogoutCommand.
/// </summary>
public sealed class LogoutCommandHandler(
    IAuthService authService,
    ILogger<LogoutCommandHandler> logger) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        var result = await authService.LogoutAsync(request.UserId, request.Jti, ct);

        if (result.IsFailure)
        {
            logger.LogWarning("{Event} | UserId:{UserId} | Error:{Error}",
                SecurityAuditEvents.TokenRevoked, request.UserId, result.Error.Code);
            return result;
        }

        logger.LogInformation("{Event} | UserId:{UserId}",
            SecurityAuditEvents.TokenRevoked, request.UserId);

        return Result.Success();
    }
}
