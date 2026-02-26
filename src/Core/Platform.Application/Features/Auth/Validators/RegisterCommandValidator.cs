using FluentValidation;
using Platform.Application.Features.Auth.Commands.Register;
using Platform.Application.Features.Auth.Constants;

namespace Platform.Application.Features.Auth.Validators;

/// <summary>
/// Validator for RegisterCommand.
/// </summary>
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(AuthValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthValidationMessages.EmailInvalid)
            .MaximumLength(256).WithMessage(AuthValidationMessages.EmailMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthValidationMessages.PasswordRequired)
            .MinimumLength(8).WithMessage(AuthValidationMessages.PasswordMinLength)
            .MaximumLength(128).WithMessage(AuthValidationMessages.PasswordMaxLength)
            .Matches(@"[a-zA-Z]").WithMessage(AuthValidationMessages.PasswordRequiresLetter)
            .Matches(@"\d").WithMessage(AuthValidationMessages.PasswordRequiresDigit);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(AuthValidationMessages.FirstNameRequired)
            .MaximumLength(100).WithMessage(AuthValidationMessages.FirstNameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(AuthValidationMessages.LastNameRequired)
            .MaximumLength(100).WithMessage(AuthValidationMessages.LastNameMaxLength);
    }
}
