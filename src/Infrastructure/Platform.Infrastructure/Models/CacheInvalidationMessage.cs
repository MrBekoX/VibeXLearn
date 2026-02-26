namespace Platform.Infrastructure.Models;

/// <summary>
/// Message payload for L1 cache invalidation via Redis Pub/Sub.
/// Broadcasts invalidation events to all application instances.
/// </summary>
public sealed record CacheInvalidationMessage
{
    /// <summary>
    /// Glob pattern for keys to invalidate (e.g., "courses:*", "categories:tree").
    /// </summary>
    public required string KeyPattern { get; init; }

    /// <summary>
    /// Unique identifier of the instance that triggered the invalidation.
    /// Used for self-invalidation guard - instances skip their own messages.
    /// </summary>
    public required string SourceInstance { get; init; }

    /// <summary>
    /// UTC timestamp when the invalidation was triggered.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}
