using System.Runtime.CompilerServices;
using Platform.Domain.Common;

namespace Platform.Domain.Common;

/// <summary>
/// Guard clauses for domain validation.
/// </summary>
public static class Guard
{
    public static class Against
    {
        /// <summary>
        /// Throws if string is null or empty.
        /// </summary>
        public static string NullOrEmpty(string value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (string.IsNullOrEmpty(value))
                throw new DomainException($"{paramName} cannot be null or empty.");
            return value;
        }

        /// <summary>
        /// Throws if string is null, empty, or whitespace.
        /// </summary>
        public static string NullOrWhiteSpace(string value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException($"{paramName} cannot be null or whitespace.");
            return value;
        }

        /// <summary>
        /// Throws if value is null.
        /// </summary>
        public static T Null<T>(T value,
            [CallerArgumentExpression("value")] string? paramName = null) where T : class
        {
            if (value is null)
                throw new DomainException($"{paramName} cannot be null.");
            return value;
        }

        /// <summary>
        /// Throws if value is default.
        /// </summary>
        public static T Default<T>(T value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
                throw new DomainException($"{paramName} cannot be default value.");
            return value;
        }

        /// <summary>
        /// Throws if value is negative or zero.
        /// </summary>
        public static decimal NegativeOrZero(decimal value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value <= 0)
                throw new DomainException($"{paramName} must be greater than zero.");
            return value;
        }

        /// <summary>
        /// Throws if value is negative.
        /// </summary>
        public static decimal Negative(decimal value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value < 0)
                throw new DomainException($"{paramName} cannot be negative.");
            return value;
        }

        /// <summary>
        /// Throws if value is out of range.
        /// </summary>
        public static decimal OutOfRange(decimal value, decimal min, decimal max,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value < min || value > max)
                throw new DomainException(
                    $"{paramName} must be between {min} and {max}. Current: {value}");
            return value;
        }

        /// <summary>
        /// Throws if value is out of range.
        /// </summary>
        public static int OutOfRange(int value, int min, int max,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value < min || value > max)
                throw new DomainException(
                    $"{paramName} must be between {min} and {max}. Current: {value}");
            return value;
        }

        /// <summary>
        /// Throws if condition is false.
        /// </summary>
        public static void False(bool condition, string message)
        {
            if (!condition)
                throw new DomainException(message);
        }

        /// <summary>
        /// Throws if condition is true.
        /// </summary>
        public static void True(bool condition, string message)
        {
            if (condition)
                throw new DomainException(message);
        }

        /// <summary>
        /// Throws if GUID is empty.
        /// </summary>
        public static Guid EmptyGuid(Guid value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value == Guid.Empty)
                throw new DomainException($"{paramName} cannot be empty.");
            return value;
        }

        /// <summary>
        /// Throws if date is in the past.
        /// </summary>
        public static DateTime InPast(DateTime value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value < DateTime.UtcNow)
                throw new DomainException($"{paramName} cannot be in the past.");
            return value;
        }
    }
}
