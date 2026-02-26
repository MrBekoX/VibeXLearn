using AutoMapper;
using Platform.Application.Features.Enrollments.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Enrollments.Mappings;

/// <summary>
/// AutoMapper profile for Enrollment feature.
/// </summary>
public sealed class EnrollmentMappingProfile : Profile
{
    public EnrollmentMappingProfile()
    {
        // Entity → GetAllEnrollmentsQueryDto
        CreateMap<Enrollment, GetAllEnrollmentsQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : string.Empty))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.CourseThumbnailUrl, opt => opt.MapFrom(s => s.Course != null ? s.Course.ThumbnailUrl : null))
            .ForMember(d => d.EnrolledAt, opt => opt.MapFrom(s => s.CreatedAt));

        // Entity → GetByIdEnrollmentQueryDto
        CreateMap<Enrollment, GetByIdEnrollmentQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.CourseThumbnailUrl, opt => opt.MapFrom(s => s.Course != null ? s.Course.ThumbnailUrl : null))
            .ForMember(d => d.EnrolledAt, opt => opt.MapFrom(s => s.CreatedAt));

        // Entity → GetByUserEnrollmentQueryDto
        CreateMap<Enrollment, GetByUserEnrollmentQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty))
            .ForMember(d => d.CourseThumbnailUrl, opt => opt.MapFrom(s => s.Course != null ? s.Course.ThumbnailUrl : null))
            .ForMember(d => d.EnrolledAt, opt => opt.MapFrom(s => s.CreatedAt));

        // Entity → GetByCourseEnrollmentQueryDto
        CreateMap<Enrollment, GetByCourseEnrollmentQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : string.Empty))
            .ForMember(d => d.EnrolledAt, opt => opt.MapFrom(s => s.CreatedAt));
    }
}
