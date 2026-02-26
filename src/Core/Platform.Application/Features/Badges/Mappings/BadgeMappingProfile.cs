using AutoMapper;
using Platform.Application.Features.Badges.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Badges.Mappings;

/// <summary>
/// AutoMapper profile for Badge feature.
/// </summary>
public sealed class BadgeMappingProfile : Profile
{
    public BadgeMappingProfile()
    {
        // Entity → GetAllBadgesQueryDto
        CreateMap<Badge, GetAllBadgesQueryDto>();

        // Entity → GetByIdBadgeQueryDto
        CreateMap<Badge, GetByIdBadgeQueryDto>()
            .ForMember(d => d.UserCount, opt => opt.MapFrom(s => s.UserBadges != null ? s.UserBadges.Count : 0));
    }
}
