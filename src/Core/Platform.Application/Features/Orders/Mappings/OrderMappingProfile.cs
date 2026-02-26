using AutoMapper;
using Platform.Application.Features.Orders.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Orders.Mappings;

/// <summary>
/// AutoMapper profile for Order feature.
/// </summary>
public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        // Entity → GetAllOrdersQueryDto
        CreateMap<Order, GetAllOrdersQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User != null ? s.User.Email ?? string.Empty : string.Empty))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.CourseThumbnailUrl, opt => opt.MapFrom(s => s.Course != null ? s.Course.ThumbnailUrl : null));

        // Entity → GetByIdOrderQueryDto
        CreateMap<Order, GetByIdOrderQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.CourseThumbnailUrl, opt => opt.MapFrom(s => s.Course != null ? s.Course.ThumbnailUrl : null))
            .ForMember(d => d.CouponCode, opt => opt.MapFrom(s => s.Coupon != null ? s.Coupon.Code : null));

        // Entity → GetByUserOrderQueryDto
        CreateMap<Order, GetByUserOrderQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.CourseThumbnailUrl, opt => opt.MapFrom(s => s.Course != null ? s.Course.ThumbnailUrl : null));

        // PaymentIntent → PaymentIntentSummaryDto
        CreateMap<PaymentIntent, PaymentIntentSummaryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}
