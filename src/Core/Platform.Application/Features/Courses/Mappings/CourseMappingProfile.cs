using AutoMapper;
using Platform.Application.Features.Courses.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Courses.Mappings;

/// <summary>
/// AutoMapper profile for Course feature.
/// </summary>
public sealed class CourseMappingProfile : Profile
{
    public CourseMappingProfile()
    {
        // Entity → GetAllCoursesQueryDto
        CreateMap<Course, GetAllCoursesQueryDto>()
            .ForMember(d => d.Level, opt => opt.MapFrom(s => s.Level.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.InstructorName, opt => opt.MapFrom(s => s.Instructor != null ? s.Instructor.FullName : null));

        // Entity → GetByIdCourseQueryDto
        CreateMap<Course, GetByIdCourseQueryDto>()
            .ForMember(d => d.Level, opt => opt.MapFrom(s => s.Level.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.InstructorName, opt => opt.MapFrom(s => s.Instructor != null ? s.Instructor.FullName : null));

        // Lesson → LessonSummaryDto
        CreateMap<Lesson, LessonSummaryDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.DurationMinutes, opt => opt.MapFrom(s => 0)); // TODO: Get from video metadata

        // Entity → GetBySlugCourseQueryDto (same as GetById)
        CreateMap<Course, GetBySlugCourseQueryDto>()
            .ForMember(d => d.Level, opt => opt.MapFrom(s => s.Level.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.InstructorName, opt => opt.MapFrom(s => s.Instructor != null ? s.Instructor.FullName : null));

        // Entity → GetByInstructorCourseQueryDto (same as GetAll)
        CreateMap<Course, GetByInstructorCourseQueryDto>()
            .ForMember(d => d.Level, opt => opt.MapFrom(s => s.Level.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : null));
    }
}
