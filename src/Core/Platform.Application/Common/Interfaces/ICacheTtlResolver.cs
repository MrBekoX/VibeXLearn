using Platform.Application.Common.Models.Cache;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Resolves L2 TTL per cache key using <see cref="CacheSettings"/>.
/// </summary>
public interface ICacheTtlResolver
{
    TimeSpan ResolveL2Duration(ICacheableQuery query);
}
