using FluentValidation;
using Platform.Application.Features.Orders.Constants;
using Platform.Application.Features.Orders.Queries.GetByIdOrder;

namespace Platform.Application.Features.Orders.Validators;

public sealed class GetByIdOrderQueryValidator : AbstractValidator<GetByIdOrderQuery>
{
    public GetByIdOrderQueryValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(OrderValidationMessages.OrderIdRequired);
    }
}
