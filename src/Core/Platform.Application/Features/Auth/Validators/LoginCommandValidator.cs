using FluentValidation;
using Platform.Application.Features.Auth.Commands.Login;
using Platform.Application.Features.Auth.Constants;

namespace Platform.Application.Features.Auth.Validators;

/// <summary>
/// Validator for LoginCommand.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(AuthValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthValidationMessages.EmailInvalid);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthValidationMessages.PasswordRequired);
    }
}
