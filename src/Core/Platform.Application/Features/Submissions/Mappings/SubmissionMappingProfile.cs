using AutoMapper;
using Platform.Application.Features.Submissions.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Submissions.Mappings;

/// <summary>
/// AutoMapper profile for Submission feature.
/// </summary>
public sealed class SubmissionMappingProfile : Profile
{
    public SubmissionMappingProfile()
    {
        // Entity → GetByIdSubmissionQueryDto
        CreateMap<Submission, GetByIdSubmissionQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : string.Empty))
            .ForMember(d => d.LessonTitle, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.Title : string.Empty))
            .ForMember(d => d.CourseId, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.CourseId : Guid.Empty))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Lesson != null && s.Lesson.Course != null ? s.Lesson.Course.Title : string.Empty));

        // Entity → GetAllSubmissionsQueryDto
        CreateMap<Submission, GetAllSubmissionsQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : string.Empty))
            .ForMember(d => d.LessonTitle, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.Title : string.Empty));

        // Entity → GetByStudentSubmissionQueryDto
        CreateMap<Submission, GetByStudentSubmissionQueryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.LessonTitle, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.Title : string.Empty))
            .ForMember(d => d.CourseId, opt => opt.MapFrom(s => s.Lesson != null ? s.Lesson.CourseId : Guid.Empty))
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.Lesson != null && s.Lesson.Course != null ? s.Lesson.Course.Title : string.Empty));
    }
}
