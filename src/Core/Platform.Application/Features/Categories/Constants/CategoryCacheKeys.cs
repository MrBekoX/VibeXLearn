using Platform.Application.Common.Models.Pagination;

namespace Platform.Application.Features.Categories.Constants;

public static class CategoryCacheKeys
{
    /// <summary>
    /// Cache key for paginated category list.
    /// </summary>
    public static string GetAll(PageRequest pr)
    {
        var search = string.IsNullOrWhiteSpace(pr.Search) ? "_" : pr.Search.ToLower().Trim();
        var sort = string.IsNullOrWhiteSpace(pr.Sort) ? "_" : pr.Sort.ToLower().Trim();
        return $"categories:list:p{pr.Page}:s{pr.PageSize}:sort:{sort}:q:{search}";
    }

    public static string GetById(Guid id) => $"categories:id:{id}";
    public static string BySlug(string slug) => $"categories:slug:{slug.ToLowerInvariant()}";
    public static string Tree() => "categories:tree";
    public static string Invalidate() => "categories:*";
}
