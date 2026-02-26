using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Constants;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IAuthService authService,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResult>>
{
    public async Task<Result<RefreshTokenResult>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request.Token, ct);

        if (result.IsFailure)
        {
            logger.LogWarning("{Event} | Error:{Error}",
                SecurityAuditEvents.TokenRefreshed, result.Error.Code);
            return Result.Fail<RefreshTokenResult>(result.Error);
        }

        var (accessToken, expiresAt, newRefreshToken) = result.Value;

        logger.LogInformation("{Event} | Token refreshed successfully",
            SecurityAuditEvents.TokenRefreshed);

        return Result.Success(new RefreshTokenResult(accessToken, expiresAt, newRefreshToken));
    }
}
