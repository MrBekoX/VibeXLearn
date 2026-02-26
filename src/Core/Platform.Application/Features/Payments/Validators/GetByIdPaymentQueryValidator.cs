using FluentValidation;
using Platform.Application.Features.Payments.Constants;
using Platform.Application.Features.Payments.Queries.GetByIdPayment;

namespace Platform.Application.Features.Payments.Validators;

public sealed class GetByIdPaymentQueryValidator : AbstractValidator<GetByIdPaymentQuery>
{
    public GetByIdPaymentQueryValidator()
    {
        RuleFor(x => x.PaymentIntentId)
            .NotEmpty().WithMessage(PaymentValidationMessages.PaymentIntentIdRequired);
    }
}
