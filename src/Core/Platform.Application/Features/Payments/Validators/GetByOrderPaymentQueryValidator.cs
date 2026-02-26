using FluentValidation;
using Platform.Application.Features.Payments.Constants;
using Platform.Application.Features.Payments.Queries.GetByOrderPayment;

namespace Platform.Application.Features.Payments.Validators;

public sealed class GetByOrderPaymentQueryValidator : AbstractValidator<GetByOrderPaymentQuery>
{
    public GetByOrderPaymentQueryValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(PaymentValidationMessages.OrderIdRequired);
    }
}
