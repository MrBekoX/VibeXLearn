using System.ComponentModel.DataAnnotations;

namespace Platform.WebAPI.Options;

/// <summary>
/// JWT configuration options.
/// SKILL: jwt-asymmetric-keys
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    /// <summary>
    /// Symmetric key secret (HS256) - used when RSA keys are not configured.
    /// Minimum 32 characters required.
    /// </summary>
    public string? Secret { get; init; }

    /// <summary>
    /// RSA private key PEM content for RS256 signing.
    /// If provided, takes precedence over Secret.
    /// </summary>
    public string? RsaPrivateKeyPem { get; init; }

    /// <summary>
    /// RSA public key PEM content for RS256 validation.
    /// If provided, takes precedence over Secret.
    /// </summary>
    public string? RsaPublicKeyPem { get; init; }

    /// <summary>
    /// Path to RSA private key file (alternative to RsaPrivateKeyPem).
    /// </summary>
    public string? RsaPrivateKeyPath { get; init; }

    /// <summary>
    /// Path to RSA public key file (alternative to RsaPublicKeyPem).
    /// </summary>
    public string? RsaPublicKeyPath { get; init; }

    /// <summary>
    /// Current key identifier for key rotation.
    /// </summary>
    public string? CurrentKeyId { get; init; }

    /// <summary>
    /// Access token lifetime in minutes. Default: 15.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    /// <summary>
    /// Refresh token lifetime in days. Default: 7.
    /// </summary>
    public int RefreshTokenLifetimeDays { get; init; } = 7;

    /// <summary>
    /// Whether to use asymmetric keys (RSA) for signing.
    /// True if RSA keys are configured, false for symmetric (HS256).
    /// </summary>
    public bool UseAsymmetricKeys =>
        !string.IsNullOrEmpty(RsaPrivateKeyPem) ||
        !string.IsNullOrEmpty(RsaPrivateKeyPath);
}
