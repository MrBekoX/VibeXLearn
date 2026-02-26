using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Auth.DTOs;

namespace Platform.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to authenticate a user and return JWT + refresh token.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<Result<LoginResult>>;

/// <summary>
/// Internal login result — includes refresh token for cookie setting.
/// The endpoint layer extracts RefreshToken and sets it as HttpOnly cookie.
/// </summary>
public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken);
