using FluentValidation;
using Platform.Application.Features.Coupons.Constants;
using Platform.Application.Features.Coupons.Queries.GetByIdCoupon;

namespace Platform.Application.Features.Coupons.Validators;

public sealed class GetByIdCouponQueryValidator : AbstractValidator<GetByIdCouponQuery>
{
    public GetByIdCouponQueryValidator()
    {
        RuleFor(x => x.CouponId)
            .NotEmpty().WithMessage(CouponValidationMessages.CouponIdRequired);
    }
}
