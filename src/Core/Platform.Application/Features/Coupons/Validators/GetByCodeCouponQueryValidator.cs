using FluentValidation;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.Queries.GetByCodeCoupon;

namespace Platform.Application.Features.Coupons.Validators;

public sealed class GetByCodeCouponQueryValidator : AbstractValidator<GetByCodeCouponQuery>
{
    public GetByCodeCouponQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(CouponValidationMessages.CodeRequired)
            .MaximumLength(50).WithMessage(CouponValidationMessages.CodeMaxLength);
    }
}
