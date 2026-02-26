using AutoMapper;
using Platform.Application.Features.Payments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Payments.Mappings;

/// <summary>
/// AutoMapper profile for Payment feature.
/// </summary>
public sealed class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        // Entity → GetAllPaymentsQueryDto
        CreateMap<PaymentIntent, GetAllPaymentsQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        // Entity → GetByIdPaymentQueryDto
        CreateMap<PaymentIntent, GetByIdPaymentQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        // Entity → GetByOrderPaymentQueryDto
        CreateMap<PaymentIntent, GetByOrderPaymentQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}
