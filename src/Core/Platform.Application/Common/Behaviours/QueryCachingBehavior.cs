using MediatR;
using Platform.Application.Common.Helpers;
using Platform.Application.Common.Interfaces;

namespace Platform.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior for automatic layered caching of queries.
/// Activated for any <see cref="IRequest{TResponse}"/> that also implements <see cref="ICacheableQuery"/>.
///
/// Flow:
///   BypassCache=true → skip read → handler → L1+L2 write → return
///   L1 HIT           → return immediately
///   L2 HIT           → L1 backfill → return
///   MISS             → handler → DB → L1+L2 write → return
///
/// Result failure (IsFailure = true) is never cached.
/// </summary>
public sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cache,
    ICacheTtlResolver ttlResolver)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var key = cacheableQuery.CacheKey;
        var l2 = ttlResolver.ResolveL2Duration(cacheableQuery);

        // ── Bypass: skip cache read, execute handler, cache on success ─────
        if (cacheableQuery.BypassCache)
        {
            var response = await next();
            if (!ResultReflectionHelper.IsFailure(response))
                await cache.SetAsync(key, response, l2, ct);
            return response;
        }

        // ── Stampede protection via GetOrSetAsync (Phase 1) ───────────────
        return await cache.GetOrSetAsync(
            key,
            async _ => await next(),
            shouldCache: r => !ResultReflectionHelper.IsFailure(r),
            l1Duration: null,
            l2Duration: l2,
            ct);
    }
}
