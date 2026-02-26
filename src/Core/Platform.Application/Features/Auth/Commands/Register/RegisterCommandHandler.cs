using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Auth.Commands.Register;

/// <summary>
/// Handler for RegisterCommand.
/// </summary>
public sealed class RegisterCommandHandler(
    IAuthService authService,
    ILogger<RegisterCommandHandler> logger) : IRequestHandler<RegisterCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(
            request.Email, request.Password, request.FirstName, request.LastName, ct);

        if (result.IsFailure)
        {
            logger.LogWarning("Registration failed for email: {Email} | Error: {Error}",
                request.Email, result.Error.Code);
            return Result.Fail<Guid>(result.Error);
        }

        logger.LogInformation("User registered successfully: {UserId} | Email: {Email}",
            result.Value, request.Email);

        return result;
    }
}
