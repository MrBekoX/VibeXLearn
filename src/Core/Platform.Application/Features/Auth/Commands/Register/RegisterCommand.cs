using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Auth.Commands.Register;

/// <summary>
/// Command to register a new user.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<Result<Guid>>;
