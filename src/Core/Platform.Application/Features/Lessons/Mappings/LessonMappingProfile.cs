using AutoMapper;
using Platform.Application.Features.Lessons.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Lessons.Mappings;

public sealed class LessonMappingProfile : Profile
{
    public LessonMappingProfile()
    {
        CreateMap<Lesson, GetByIdLessonQueryDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Course != null ? s.Course.Title : string.Empty));

        CreateMap<Lesson, GetByCourseLessonQueryDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));
    }
}
