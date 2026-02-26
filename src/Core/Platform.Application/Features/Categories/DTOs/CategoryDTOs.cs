namespace Platform.Application.Features.Categories.DTOs;

public sealed record GetAllCategoriesQueryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public int CourseCount { get; init; }
}

public sealed record GetByIdCategoryQueryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public DateTime CreatedAt { get; init; }
    public IList<GetAllCategoriesQueryDto> Children { get; init; } = [];
}

public sealed record CategoryTreeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public IList<CategoryTreeDto> Children { get; init; } = [];
}

public sealed record CreateCategoryCommandDto
{
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
}

public sealed record UpdateCategoryCommandDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
}

public sealed record GetBySlugCategoryQueryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public DateTime CreatedAt { get; init; }
    public IList<GetAllCategoriesQueryDto> Children { get; init; } = [];
}
