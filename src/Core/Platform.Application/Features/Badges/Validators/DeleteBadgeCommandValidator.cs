using FluentValidation;
using Platform.Application.Features.Badges.Commands.DeleteBadge;
using Platform.Application.Features.Badges.Constants;

namespace Platform.Application.Features.Badges.Validators;

/// <summary>
/// Validator for DeleteBadgeCommand.
/// </summary>
public sealed class DeleteBadgeCommandValidator : AbstractValidator<DeleteBadgeCommand>
{
    public DeleteBadgeCommandValidator()
    {
        RuleFor(x => x.BadgeId)
            .NotEmpty().WithMessage(BadgeValidationMessages.BadgeIdEmpty);
    }
}
