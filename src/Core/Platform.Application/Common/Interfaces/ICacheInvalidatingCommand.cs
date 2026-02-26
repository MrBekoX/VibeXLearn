namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that should invalidate cache entries on success.
/// Command records implementing this interface are automatically handled
/// by <c>CommandCacheInvalidationBehavior</c>.
/// </summary>
public interface ICacheInvalidatingCommand
{
    /// <summary>
    /// Redis key patterns to remove after the command succeeds.
    /// Uses glob-style wildcards (e.g., "courses:*", "enrollments:user:{id}:*").
    /// </summary>
    IReadOnlyList<string> CacheInvalidationPatterns { get; }
}
