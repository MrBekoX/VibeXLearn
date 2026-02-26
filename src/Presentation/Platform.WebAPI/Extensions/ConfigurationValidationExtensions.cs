using Microsoft.AspNetCore.Builder;

namespace Platform.WebAPI.Extensions;

/// <summary>
/// Configuration validation extensions.
/// SKILL: secrets-from-config
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Validates critical configuration values at startup.
    /// In production, all secrets MUST come from environment variables.
    /// </summary>
    public static void ValidateConfiguration(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var env = builder.Environment;

        var requiredKeys = new[]
        {
            "ConnectionStrings:DefaultConnection",
            "ConnectionStrings:Redis",
            "Jwt:Secret",
            "Jwt:Issuer",
            "Jwt:Audience"
        };

        var warnings = new List<string>();
        var errors = new List<string>();

        foreach (var key in requiredKeys)
        {
            var value = config[key];
            if (string.IsNullOrEmpty(value))
            {
                if (env.IsProduction())
                {
                    errors.Add($"{key} is required in production.");
                }
                else
                {
                    warnings.Add($"{key} is not configured.");
                }
            }
            else if (value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
                     value.Contains("your_", StringComparison.OrdinalIgnoreCase) ||
                     value.Contains("dev_password", StringComparison.OrdinalIgnoreCase))
            {
                if (env.IsProduction())
                {
                    errors.Add($"{key} contains placeholder value in production.");
                }
                else
                {
                    warnings.Add($"{key} uses placeholder value (acceptable in development).");
                }
            }
        }

        // Validate JWT secret length
        var jwtSecret = config["Jwt:Secret"];
        if (!string.IsNullOrEmpty(jwtSecret) && jwtSecret.Length < 32)
        {
            errors.Add("Jwt:Secret must be at least 32 characters for HS256 security.");
        }

        // Log warnings
        foreach (var warning in warnings)
        {
            Console.WriteLine($"[CONFIG WARNING] {warning}");
        }

        // Throw on errors in production
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuration validation failed:\n" + string.Join("\n", errors));
        }

        // Validate Iyzico configuration if enabled
        var iyzicoApiKey = config["Iyzico:ApiKey"];
        if (!string.IsNullOrEmpty(iyzicoApiKey))
        {
            var iyzicoSecretKey = config["Iyzico:SecretKey"];
            if (string.IsNullOrEmpty(iyzicoSecretKey))
            {
                throw new InvalidOperationException(
                    "Iyzico:SecretKey is required when Iyzico:ApiKey is configured.");
            }

            // Validate callback URL is HTTPS in production
            var callbackUrl = config["Iyzico:CallbackUrl"];
            if (env.IsProduction() &&
                !string.IsNullOrEmpty(callbackUrl) &&
                !callbackUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Iyzico:CallbackUrl must use HTTPS in production.");
            }
        }
    }
}
