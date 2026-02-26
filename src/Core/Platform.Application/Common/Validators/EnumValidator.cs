using FluentValidation;

namespace Platform.Application.Common.Validators;

/// <summary>
/// Extension methods for enum validation in FluentValidation.
/// SKILL: enum-validation
/// </summary>
public static class EnumValidatorExtensions
{
    /// <summary>
    /// Validates that the string value is a valid enum value.
    /// </summary>
    /// <typeparam name="T">The object type being validated</typeparam>
    /// <typeparam name="TEnum">The enum type to validate against</typeparam>
    /// <param name="ruleBuilder">The rule builder</param>
    /// <param name="caseSensitive">Whether comparison should be case sensitive (default: false)</param>
    /// <returns>The rule builder options</returns>
    public static IRuleBuilderOptions<T, string?> IsEnumName<T, TEnum>(
        this IRuleBuilder<T, string?> ruleBuilder,
        bool caseSensitive = false)
        where TEnum : struct, Enum
    {
        return ruleBuilder
            .Must((root, value, context) =>
            {
                if (string.IsNullOrEmpty(value))
                    return true; // Use .NotEmpty() for required validation

                return Enum.TryParse<TEnum>(value, !caseSensitive, out _);
            })
            .WithMessage((root, value) =>
            {
                var validValues = string.Join(", ", Enum.GetNames<TEnum>());
                return $"'{{PropertyName}}' must be one of: {validValues}. You provided: '{value}'";
            });
    }

    /// <summary>
    /// Validates that the enum value is defined (not an invalid numeric cast).
    /// </summary>
    public static IRuleBuilderOptions<T, TEnum> IsDefined<T, TEnum>(
        this IRuleBuilder<T, TEnum> ruleBuilder)
        where TEnum : struct, Enum
    {
        return ruleBuilder
            .Must(value => Enum.IsDefined(typeof(TEnum), value))
            .WithMessage($"'{{PropertyName}}' must be a valid value: {string.Join(", ", Enum.GetNames<TEnum>())}");
    }
}
