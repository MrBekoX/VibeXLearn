using AutoMapper;
using Platform.Application.Features.LiveSessions.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.LiveSessions.Mappings;

/// <summary>
/// AutoMapper profile for LiveSession feature.
/// </summary>
public sealed class LiveSessionMappingProfile : Profile
{
    public LiveSessionMappingProfile()
    {
        // Entity → GetByIdLiveSessionQueryDto
        CreateMap<LiveSession, GetByIdLiveSessionQueryDto>()
            .ForMember(d => d.HostUrl, opt => opt.MapFrom(s => s.StartUrl))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.LessonTitle, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.Title : string.Empty))
            .ForMember(d => d.CourseId, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.CourseId : Guid.Empty))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Lesson != null && s.Lesson.Course != null ? s.Lesson.Course.Title : string.Empty));

        // Entity → GetAllLiveSessionsQueryDto
        CreateMap<LiveSession, GetAllLiveSessionsQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.LessonTitle, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.Title : string.Empty));

        // Entity → UpcomingLiveSessionQueryDto
        CreateMap<LiveSession, UpcomingLiveSessionQueryDto>()
            .ForMember(d => d.LessonTitle, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.Title : string.Empty))
            .ForMember(d => d.CourseId, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.CourseId : Guid.Empty))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Lesson != null && s.Lesson.Course != null ? s.Lesson.Course.Title : string.Empty));
    }
}
