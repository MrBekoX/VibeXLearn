using Platform.Application.Common.Models.Pagination;

namespace Platform.Application.Features.Courses.Constants;

/// <summary>
/// Course cache key definitions.
/// </summary>
public static class CourseCacheKeys
{
    /// <summary>
    /// Cache key for paginated course list.
    /// </summary>
    public static string GetAll(PageRequest pr)
    {
        var search = string.IsNullOrWhiteSpace(pr.Search) ? "_" : pr.Search.ToLower().Trim();
        var sort = string.IsNullOrWhiteSpace(pr.Sort) ? "_" : pr.Sort.ToLower().Trim();
        return $"courses:list:p{pr.Page}:s{pr.PageSize}:sort:{sort}:q:{search}";
    }

    /// <summary>
    /// Cache key for single course by ID.
    /// </summary>
    public static string GetById(Guid id) => $"courses:id:{id}";

    /// <summary>
    /// Cache key for single course by slug.
    /// </summary>
    public static string BySlug(string slug) => $"courses:slug:{slug.ToLowerInvariant()}";

    /// <summary>
    /// Cache key for paginated courses by instructor.
    /// </summary>
    public static string ByInstructor(Guid instructorId, PageRequest pr)
    {
        var normalized = pr.Normalize();
        return $"courses:instructor:{instructorId}:p{normalized.Page}:s{normalized.PageSize}";
    }

    /// <summary>
    /// Pattern for invalidating all course cache entries.
    /// </summary>
    public static string InvalidateAll() => "courses:*";

    /// <summary>
    /// Pattern for invalidating course list caches.
    /// </summary>
    public static string InvalidateLists() => "courses:list:*";
}
