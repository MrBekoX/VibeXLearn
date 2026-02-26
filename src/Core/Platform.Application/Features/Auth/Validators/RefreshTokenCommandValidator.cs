using FluentValidation;
using Platform.Application.Features.Auth.Commands.RefreshToken;
using Platform.Application.Features.Auth.Constants;

namespace Platform.Application.Features.Auth.Validators;

/// <summary>
/// Validator for RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(AuthValidationMessages.RefreshTokenRequired);
    }
}
