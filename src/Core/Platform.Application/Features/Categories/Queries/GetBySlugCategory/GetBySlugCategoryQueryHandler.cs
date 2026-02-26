using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Categories.Constants;
using Platform.Application.Features.Categories.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Categories.Queries.GetBySlugCategory;

public sealed class GetBySlugCategoryQueryHandler(
    IReadRepository<Category> repo)
    : IRequestHandler<GetBySlugCategoryQuery, Result<GetByIdCategoryQueryDto>>
{
    public async Task<Result<GetByIdCategoryQueryDto>> Handle(
        GetBySlugCategoryQuery request,
        CancellationToken ct)
    {
        var normalizedSlug = request.Slug.ToLowerInvariant();

        var category = await repo.GetAsync(
            c => c.Slug == normalizedSlug,
            ct,
            includes: [c => c.Parent!, c => c.Children]);

        if (category is null)
            return Result.Fail<GetByIdCategoryQueryDto>("CATEGORY_NOT_FOUND", CategoryBusinessMessages.NotFound);

        var dto = new GetByIdCategoryQueryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            ParentName = category.Parent?.Name,
            CreatedAt = category.CreatedAt,
            Children = category.Children?.Select(ch => new GetAllCategoriesQueryDto
            {
                Id = ch.Id,
                Name = ch.Name,
                Slug = ch.Slug,
                Description = ch.Description,
                CourseCount = ch.Courses?.Count ?? 0
            }).ToList() ?? []
        };

        return Result.Success(dto);
    }
}
