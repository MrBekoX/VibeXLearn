using Platform.Application.Common.Models.Pagination;

namespace Platform.Application.Features.Badges.Constants;

/// <summary>
/// Badge cache key definitions.
/// </summary>
public static class BadgeCacheKeys
{
    /// <summary>
    /// Cache key for paginated badge list.
    /// </summary>
    public static string GetAll(int page, int pageSize) => $"badges:p{page}:s{pageSize}";

    /// <summary>
    /// Cache key for single badge by ID.
    /// </summary>
    public static string GetById(Guid id) => $"badges:id:{id}";

    /// <summary>
    /// Pattern for invalidating all badge cache entries.
    /// </summary>
    public static string InvalidateAll() => "badges:*";

    /// <summary>
    /// Pattern for invalidating badge list caches.
    /// </summary>
    public static string InvalidateLists() => "badges:p*";
}
