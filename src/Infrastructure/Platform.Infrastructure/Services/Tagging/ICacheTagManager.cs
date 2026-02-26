namespace Platform.Infrastructure.Services.Tagging;

/// <summary>
/// Manages cache key tagging for efficient bulk invalidation.
/// Uses Redis Sets to maintain tag → keys mappings.
/// </summary>
public interface ICacheTagManager
{
    /// <summary>
    /// Associates a cache key with one or more tags.
    /// Each tag maintains a Redis SET of associated keys.
    /// </summary>
    /// <param name="key">The cache key to tag.</param>
    /// <param name="tags">Tags to associate with the key.</param>
    /// <param name="tagTtl">TTL for the tag SET (refreshed on each association).</param>
    /// <param name="ct">Cancellation token.</param>
    Task AssociateTagsAsync(string key, IEnumerable<string> tags, TimeSpan tagTtl, CancellationToken ct = default);

    /// <summary>
    /// Gets all cache keys associated with a tag.
    /// </summary>
    /// <param name="tag">The tag to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of cache keys associated with the tag.</returns>
    Task<IReadOnlyList<string>> GetKeysByTagAsync(string tag, CancellationToken ct = default);

    /// <summary>
    /// Removes orphaned keys from a tag SET.
    /// An orphan key is referenced in the tag SET but no longer exists in Redis.
    /// </summary>
    /// <param name="keys">Keys to check for existence.</param>
    /// <param name="tag">The tag to clean up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of orphan keys removed.</returns>
    Task<int> CleanupOrphanKeysAsync(IEnumerable<string> keys, string tag, CancellationToken ct = default);

    /// <summary>
    /// Removes a specific key from all tag SETs it belongs to.
    /// </summary>
    /// <param name="key">The key to remove from tags.</param>
    /// <param name="tags">The tags to remove the key from.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveKeyFromTagsAsync(string key, IEnumerable<string> tags, CancellationToken ct = default);
}
