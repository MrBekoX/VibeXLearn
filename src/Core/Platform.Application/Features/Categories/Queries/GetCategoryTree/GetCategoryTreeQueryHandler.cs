using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Categories.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Categories.Queries.GetCategoryTree;

/// <summary>
/// Handler for GetCategoryTreeQuery.
/// Cache is managed automatically by QueryCachingBehavior — no manual cache calls needed.
/// </summary>
public sealed class GetCategoryTreeQueryHandler(
    IReadRepository<Category> repo)
    : IRequestHandler<GetCategoryTreeQuery, Result<IList<CategoryTreeDto>>>
{
    public async Task<Result<IList<CategoryTreeDto>>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken ct)
    {
        var allCategories = await repo.GetAllAsync(ct);

        IList<CategoryTreeDto> BuildTree(Guid? parentId) =>
            allCategories
                .Where(c => c.ParentId == parentId)
                .Select(c => new CategoryTreeDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Children = BuildTree(c.Id)
                }).ToList();

        var result = BuildTree(null);
        return Result.Success(result);
    }
}
