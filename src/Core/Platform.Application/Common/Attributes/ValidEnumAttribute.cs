using System.ComponentModel.DataAnnotations;

namespace Platform.Application.Common.Attributes;

/// <summary>
/// Validates that a string is a valid enum value.
/// SKILL: enum-validation
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ValidEnumAttribute : ValidationAttribute
{
    private readonly Type _enumType;

    public ValidEnumAttribute(Type enumType)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException($"Type {enumType.Name} must be an enum type.", nameof(enumType));

        _enumType = enumType;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "")
            return ValidationResult.Success; // Use [Required] for required validation

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return ValidationResult.Success;

        if (Enum.GetNames(_enumType).Any(name =>
            string.Equals(name, stringValue, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationResult.Success;
        }

        var validValues = string.Join(", ", Enum.GetNames(_enumType));
        return new ValidationResult(
            $"Invalid value for {validationContext.DisplayName}. Valid values: {validValues}");
    }
}
