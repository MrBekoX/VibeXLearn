using MediatR;
using Platform.Application.Common.Helpers;
using Platform.Application.Common.Interfaces;

namespace Platform.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior for automatic cache invalidation after successful commands.
/// Activated for any <see cref="IRequest{TResponse}"/> that also implements
/// <see cref="ICacheInvalidatingCommand"/>.
///
/// Flow:
///   Command → Handler → SUCCESS → resolve patterns → RemoveByPatternAsync(patterns[]) in parallel
///                     → FAILURE → no cache touched (stale cache preferable to broken state)
///
/// Commands implementing <see cref="IResolvableCacheInvalidatingCommand"/> resolve their
/// own patterns dynamically (e.g., by loading related entities from the database).
/// All other commands use the static <see cref="ICacheInvalidatingCommand.CacheInvalidationPatterns"/>.
///
/// Pattern errors are logged but not rethrown — cache miss on next read is acceptable.
/// </summary>
public sealed class CommandCacheInvalidationBehavior<TRequest, TResponse>(
    ICacheService cache,
    IServiceProvider serviceProvider,
    ILogger<CommandCacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheInvalidatingCommand invalidatingCommand)
            return await next();

        var response = await next();

        // Only invalidate on success
        if (ResultReflectionHelper.IsFailureOrDefault(response, nullMeansFailure: false))
            return response;

        var patterns = await ResolvePatternsAsync(invalidatingCommand, ct);
        if (patterns.Count == 0)
            return response;

        // Fire invalidations in parallel — pattern errors logged, not thrown
        var tasks = patterns.Select(async pattern =>
        {
            try
            {
                await cache.RemoveByPatternAsync(pattern, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Cache invalidation failed for pattern {Pattern}. Cache may be stale.", pattern);
            }
        });

        await Task.WhenAll(tasks);

        return response;
    }

    private async Task<IReadOnlyList<string>> ResolvePatternsAsync(
        ICacheInvalidatingCommand invalidatingCommand,
        CancellationToken ct)
    {
        // Commands that need dynamic resolution (DB lookups, etc.) implement
        // IResolvableCacheInvalidatingCommand and resolve their own patterns.
        if (invalidatingCommand is IResolvableCacheInvalidatingCommand resolvable)
            return await resolvable.ResolvePatternsAsync(serviceProvider, ct);

        // Static patterns — no resolution needed
        return invalidatingCommand.CacheInvalidationPatterns
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
