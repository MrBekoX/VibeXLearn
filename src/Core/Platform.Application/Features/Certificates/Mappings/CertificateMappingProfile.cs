using AutoMapper;
using Platform.Application.Features.Certificates.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Certificates.Mappings;

/// <summary>
/// AutoMapper profile for Certificate feature.
/// </summary>
public sealed class CertificateMappingProfile : Profile
{
    public CertificateMappingProfile()
    {
        // Entity → GetByIdCertificateQueryDto
        CreateMap<Certificate, GetByIdCertificateQueryDto>()
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : string.Empty))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        // Entity → GetAllCertificatesQueryDto
        CreateMap<Certificate, GetAllCertificatesQueryDto>()
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : string.Empty))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        // Entity → GetByUserCertificateQueryDto
        CreateMap<Certificate, GetByUserCertificateQueryDto>()
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}
