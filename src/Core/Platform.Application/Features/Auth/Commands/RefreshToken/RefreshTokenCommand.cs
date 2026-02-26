using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an access token using a refresh token.
/// </summary>
public sealed record RefreshTokenCommand(string Token) : IRequest<Result<RefreshTokenResult>>;

/// <summary>
/// Internal refresh result — includes new refresh token for cookie rotation.
/// </summary>
public sealed record RefreshTokenResult(
    string AccessToken,
    DateTime ExpiresAt,
    string NewRefreshToken);
