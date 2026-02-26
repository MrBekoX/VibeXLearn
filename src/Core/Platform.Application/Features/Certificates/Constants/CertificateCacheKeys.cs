namespace Platform.Application.Features.Certificates.Constants;

/// <summary>
/// Certificate cache key definitions.
/// </summary>
public static class CertificateCacheKeys
{
    /// <summary>
    /// Cache key for single certificate by ID.
    /// </summary>
    public static string GetById(Guid id) => $"certificates:id:{id}";

    /// <summary>
    /// Cache key for certificates by user.
    /// </summary>
    public static string ByUser(Guid userId) => $"certificates:user:{userId}";

    /// <summary>
    /// Pattern for invalidating all certificate cache entries.
    /// </summary>
    public static string InvalidateAll() => "certificates:*";
}
