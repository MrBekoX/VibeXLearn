using AutoMapper;
using Platform.Application.Features.Categories.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Categories.Mappings;

public sealed class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Category, GetAllCategoriesQueryDto>()
            .ForMember(d => d.ParentName, opt => opt.MapFrom(s => s.Parent != null ? s.Parent.Name : null))
            .ForMember(d => d.CourseCount, opt => opt.MapFrom(s => s.Courses != null ? s.Courses.Count : 0));
        CreateMap<Category, GetByIdCategoryQueryDto>()
            .ForMember(d => d.ParentName, opt => opt.MapFrom(s => s.Parent != null ? s.Parent.Name : null));
        CreateMap<Category, CategoryTreeDto>();
    }
}
