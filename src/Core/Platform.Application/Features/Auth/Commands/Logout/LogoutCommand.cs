using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Command to revoke all refresh tokens and blacklist current access token (logout).
/// </summary>
public sealed record LogoutCommand(Guid UserId, string? Jti) : IRequest<Result>;
