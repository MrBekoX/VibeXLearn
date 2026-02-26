using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Platform.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that masks sensitive data in log events.
/// Prevents passwords, tokens, API keys, card numbers from leaking to logs.
/// SKILL: fix-logging-security
/// </summary>
public class SensitiveDataEnricher : ILogEventEnricher
{
    // Patterns to detect and mask sensitive data
    private static readonly Regex[] SensitivePatterns =
    [
        // JSON patterns
        new(@"""password""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""token""\s*:\s*""[^""]{8,}""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""secretKey""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""apiKey""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""cardNumber""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""cvv""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""pan""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""iban""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""tckn""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""identityNumber""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""refreshToken""\s*:\s*""[^""]{8,}""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""accessToken""\s*:\s*""[^""]{8,}""", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Key=value patterns
        new(@"password=[^\s&]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"token=[^\s&]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"secret=[^\s&]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"api_key=[^\s&]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Credit card pattern (13-19 digits)
        new(@"\b\d{13,19}\b", RegexOptions.Compiled),

        // Email pattern for potential PII (optional - uncomment if needed)
        // new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled),

        // JWT token pattern (three base64 parts separated by dots)
        new(@"eyJ[A-Za-z0-9_-]*\.eyJ[A-Za-z0-9_-]*\.[A-Za-z0-9_-]*", RegexOptions.Compiled)
    ];

    // Keys that should be completely masked
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "pwd", "pass", "secret", "secretkey", "apikey", "api_key",
        "token", "accesstoken", "access_token", "refreshtoken", "refresh_token",
        "authorization", "cardnumber", "cvv", "pan", "iban", "tckn", "identitynumber",
        "privatekey", "private_key", "credential"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToList())
        {
            if (property.Value is ScalarValue scalarValue &&
                scalarValue.Value is string stringValue)
            {
                // Check if property name is sensitive
                if (SensitiveKeys.Contains(property.Key))
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(
                        property.Key,
                        new ScalarValue("***MASKED***")));
                    continue;
                }

                // Mask sensitive patterns in the value
                var maskedValue = MaskSensitiveData(stringValue);
                if (maskedValue != stringValue)
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(
                        property.Key,
                        new ScalarValue(maskedValue)));
                }
            }
        }
    }

    private static string MaskSensitiveData(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        foreach (var pattern in SensitivePatterns)
        {
            value = pattern.Replace(value, match =>
            {
                var original = match.Value;

                // For JSON key:value patterns
                if (original.Contains(':'))
                {
                    var colonIndex = original.IndexOf(':');
                    var key = original[..(colonIndex + 1)];
                    return $"{key} \"***\"";
                }

                // For key=value patterns
                if (original.Contains('='))
                {
                    var equalIndex = original.IndexOf('=');
                    var key = original[..(equalIndex + 1)];
                    return $"{key}***";
                }

                // For patterns like credit cards or tokens
                if (original.Length > 8)
                {
                    return original[..4] + "***" + original[^4..];
                }

                return "***";
            });
        }

        return value;
    }
}
