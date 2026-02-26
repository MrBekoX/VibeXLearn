namespace Platform.WebAPI.Helpers;

/// <summary>
/// Helper methods for safe enum parsing in endpoints.
/// SKILL: enum-validation
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Safely parses an enum value and returns an error object if invalid.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to parse</typeparam>
    /// <param name="value">The string value to parse</param>
    /// <param name="fieldName">The field name for error messages</param>
    /// <param name="result">The parsed enum value if successful</param>
    /// <returns>null if successful, error object if failed</returns>
    public static object? TryParseEnum<TEnum>(
        string? value,
        string fieldName,
        out TEnum result) where TEnum : struct, Enum
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return new
            {
                error = $"{fieldName} is required",
                field = fieldName.ToLowerInvariant()
            };
        }

        if (Enum.TryParse<TEnum>(value, true, out var parsed))
        {
            result = parsed;
            return null; // Success
        }

        // Return error object
        var validValues = string.Join(", ", Enum.GetNames<TEnum>());
        return new
        {
            error = $"Invalid {fieldName.ToLowerInvariant()}",
            field = fieldName.ToLowerInvariant(),
            value = value,
            validValues = validValues
        };
    }

    /// <summary>
    /// Gets all valid values for an enum type as comma-separated string.
    /// </summary>
    public static string GetValidValues<TEnum>() where TEnum : struct, Enum
    {
        return string.Join(", ", Enum.GetNames<TEnum>());
    }

    /// <summary>
    /// Checks if a string is a valid enum value.
    /// </summary>
    public static bool IsValidEnum<TEnum>(string? value, bool ignoreCase = true) where TEnum : struct, Enum
    {
        return !string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, ignoreCase, out _);
    }
}
