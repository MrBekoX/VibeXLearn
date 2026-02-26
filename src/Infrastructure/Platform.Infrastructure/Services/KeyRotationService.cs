using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Manages JWT signing keys with rotation support.
/// SKILL: jwt-asymmetric-keys
/// </summary>
public sealed class KeyRotationService : IHostedService
{
    private readonly Dictionary<string, SigningCredentials> _signingKeys = new();
    private readonly Dictionary<string, SecurityKey> _validationKeys = new();
    private readonly ILogger<KeyRotationService> _logger;
    private readonly IConfiguration _config;
    private readonly object _initLock = new();
    private volatile bool _isInitialized;

    private string _currentKeyId = default!;
    private SigningCredentials? _currentSigningCredentials;
    private SecurityKey? _currentValidationKey;

    public KeyRotationService(
        IConfiguration config,
        ILogger<KeyRotationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current key ID used for signing.
    /// </summary>
    public string CurrentKeyId
    {
        get
        {
            EnsureInitialized();
            return _currentKeyId;
        }
    }

    /// <summary>
    /// Gets the current signing credentials for token generation.
    /// </summary>
    public SigningCredentials CurrentSigningCredentials
    {
        get
        {
            EnsureInitialized();
            return _currentSigningCredentials
                ?? throw new InvalidOperationException("Signing key is not initialized.");
        }
    }

    /// <summary>
    /// Gets the current validation key for token validation.
    /// </summary>
    public SecurityKey CurrentValidationKey
    {
        get
        {
            EnsureInitialized();
            return _currentValidationKey
                ?? throw new InvalidOperationException("Validation key is not initialized.");
        }
    }

    /// <summary>
    /// Checks if a key with the given ID exists.
    /// </summary>
    public bool KeyExists(string? kid)
    {
        EnsureInitialized();
        return !string.IsNullOrEmpty(kid) && _validationKeys.ContainsKey(kid);
    }

    /// <summary>
    /// Gets the validation key by ID for token validation.
    /// </summary>
    public SecurityKey? GetValidationKey(string? kid)
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(kid))
            return _currentValidationKey;

        return _validationKeys.TryGetValue(kid, out var key) ? key : null;
    }

    /// <summary>
    /// Gets all available key IDs for JWKS endpoint.
    /// </summary>
    public IEnumerable<string> GetAllKeyIds()
    {
        EnsureInitialized();
        return _validationKeys.Keys;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        EnsureInitialized();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        lock (_initLock)
        {
            if (_isInitialized)
                return;

            InitializeKeys();
            _isInitialized = true;
        }
    }

    private void InitializeKeys()
    {
        try
        {
            var rsaPrivateKeyPem = GetPrivateKeyPem();
            var rsaPublicKeyPem = GetPublicKeyPem();

            if (!string.IsNullOrEmpty(rsaPrivateKeyPem) && !string.IsNullOrEmpty(rsaPublicKeyPem))
            {
                InitializeAsymmetricKeys(rsaPrivateKeyPem, rsaPublicKeyPem);
            }
            else
            {
                // Fallback to symmetric key
                InitializeSymmetricKey();
            }

            _logger.LogInformation(
                "Key rotation initialized. KeyId: {KeyId}, Type: {KeyType}",
                _currentKeyId,
                _currentSigningCredentials?.Algorithm ?? "HS256");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize JWT keys");
            throw;
        }
    }

    private void InitializeAsymmetricKeys(string privateKeyPem, string publicKeyPem)
    {
        var rsa = RSA.Create();

        // Import private key for signing
        rsa.ImportFromPem(privateKeyPem);

        var rsaSecurityKey = new RsaSecurityKey(rsa)
        {
            KeyId = _config["Jwt:CurrentKeyId"] ?? GenerateKeyId()
        };

        _currentKeyId = rsaSecurityKey.KeyId;
        _currentSigningCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

        // Import public key for validation
        var rsaPublic = RSA.Create();
        rsaPublic.ImportFromPem(publicKeyPem);

        var publicKey = new RsaSecurityKey(rsaPublic)
        {
            KeyId = _currentKeyId
        };

        _currentValidationKey = publicKey;

        _signingKeys[_currentKeyId] = _currentSigningCredentials;
        _validationKeys[_currentKeyId] = _currentValidationKey;

        _logger.LogInformation("RSA asymmetric keys initialized. KeyId: {KeyId}", _currentKeyId);
    }

    private void InitializeSymmetricKey()
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "Jwt:Secret is required when RSA keys are not configured.");

        if (secret.Length < 32)
            throw new InvalidOperationException(
                $"Jwt:Secret must be at least 32 characters. Current: {secret.Length}");

        var keyBytes = System.Text.Encoding.UTF8.GetBytes(secret);
        var keyId = _config["Jwt:CurrentKeyId"] ?? GenerateKeyId();

        var securityKey = new SymmetricSecurityKey(keyBytes) { KeyId = keyId };
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        _currentKeyId = keyId;
        _currentSigningCredentials = credentials;
        _currentValidationKey = securityKey;

        _signingKeys[keyId] = credentials;
        _validationKeys[keyId] = securityKey;

        _logger.LogInformation("Symmetric key initialized. KeyId: {KeyId}", _currentKeyId);
    }

    private string? GetPrivateKeyPem()
    {
        var pemContent = _config["Jwt:RsaPrivateKeyPem"];
        if (!string.IsNullOrEmpty(pemContent))
            return pemContent;

        var filePath = _config["Jwt:RsaPrivateKeyPath"];
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            return File.ReadAllText(filePath);

        return null;
    }

    private string? GetPublicKeyPem()
    {
        var pemContent = _config["Jwt:RsaPublicKeyPem"];
        if (!string.IsNullOrEmpty(pemContent))
            return pemContent;

        var filePath = _config["Jwt:RsaPublicKeyPath"];
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            return File.ReadAllText(filePath);

        return null;
    }

    private static string GenerateKeyId()
    {
        return $"key_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid():N}"[..16];
    }
}
