using FluentValidation;
using Platform.Application.Features.LiveSessions.Constants;
using Platform.Application.Features.LiveSessions.Queries.GetByIdLiveSession;

namespace Platform.Application.Features.LiveSessions.Validators;

/// <summary>
/// Validator for GetByIdLiveSessionQuery.
/// </summary>
public sealed class GetByIdLiveSessionQueryValidator : AbstractValidator<GetByIdLiveSessionQuery>
{
    public GetByIdLiveSessionQueryValidator()
    {
        RuleFor(x => x.LiveSessionId)
            .NotEmpty().WithMessage(LiveSessionValidationMessages.LiveSessionIdEmpty);
    }
}
