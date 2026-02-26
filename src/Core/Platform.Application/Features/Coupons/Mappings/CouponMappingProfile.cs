using AutoMapper;
using Platform.Application.Features.Coupons.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Coupons.Mappings;

/// <summary>
/// AutoMapper profile for Coupon feature.
/// </summary>
public sealed class CouponMappingProfile : Profile
{
    public CouponMappingProfile()
    {
        // Entity → GetAllCouponsQueryDto
        CreateMap<Coupon, GetAllCouponsQueryDto>();

        // Entity → GetByIdCouponQueryDto
        CreateMap<Coupon, GetByIdCouponQueryDto>();
    }
}
