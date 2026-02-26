namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Marker interface for cacheable queries.
/// Query records implementing this interface are automatically handled
/// by <c>QueryCachingBehavior</c> — no manual cache calls needed in handlers.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Unique Redis key for this query result.</summary>
    string CacheKey { get; }

    /// <summary>L2 (Redis) TTL. L1 (Memory) TTL = L2 × L1ToL2Ratio.</summary>
    TimeSpan L2Duration { get; }

    /// <summary>
    /// When true, skips both L1 and L2 read and always hits the DB.
    /// The response is still written to cache after the handler completes.
    /// </summary>
    bool BypassCache { get; }
}
