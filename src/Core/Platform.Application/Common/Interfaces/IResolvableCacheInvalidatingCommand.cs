namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Extension of <see cref="ICacheInvalidatingCommand"/> for commands whose cache
/// invalidation patterns depend on runtime data (e.g., loading a related entity
/// from the database to resolve user-specific or course-specific patterns).
/// <para>
/// When a command implements this interface, <c>CommandCacheInvalidationBehavior</c>
/// calls <see cref="ResolvePatternsAsync"/> instead of using the static
/// <see cref="ICacheInvalidatingCommand.CacheInvalidationPatterns"/> property.
/// </para>
/// </summary>
public interface IResolvableCacheInvalidatingCommand : ICacheInvalidatingCommand
{
    /// <summary>
    /// Resolves cache invalidation patterns asynchronously after the command succeeds.
    /// Use this to load related entities and build fine-grained patterns
    /// instead of broad wildcards.
    /// </summary>
    Task<IReadOnlyList<string>> ResolvePatternsAsync(
        IServiceProvider serviceProvider,
        CancellationToken ct);
}
