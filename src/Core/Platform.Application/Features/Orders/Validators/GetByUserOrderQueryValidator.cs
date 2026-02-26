using FluentValidation;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.Queries.GetByUserOrder;

namespace Platform.Application.Features.Orders.Validators;

public sealed class GetByUserOrderQueryValidator : AbstractValidator<GetByUserOrderQuery>
{
    public GetByUserOrderQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(OrderValidationMessages.UserIdRequired);
    }
}
