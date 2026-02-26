using System.Collections.Concurrent;
using System.Reflection;

namespace Platform.Application.Common.Helpers;

/// <summary>
/// Caches <c>IsFailure</c> property lookups to avoid repeated reflection overhead
/// in pipeline behaviors.
/// </summary>
internal static class ResultReflectionHelper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> Cache = new();

    /// <summary>
    /// Returns true when <paramref name="response"/> is null or its <c>IsFailure</c> property is true.
    /// Works for both <c>Result</c> and <c>Result&lt;T&gt;</c>.
    /// </summary>
    internal static bool IsFailure<T>(T? response)
    {
        if (response is null) return true;
        var prop = Cache.GetOrAdd(response.GetType(),
            t => t.GetProperty(nameof(Results.Result.IsFailure)));
        return prop?.GetValue(response) is true;
    }

    /// <summary>
    /// Same as <see cref="IsFailure{T}"/> but treats null as success (for void/unit commands).
    /// </summary>
    internal static bool IsFailureOrDefault<T>(T? response, bool nullMeansFailure = true)
    {
        if (response is null) return nullMeansFailure;
        var prop = Cache.GetOrAdd(response.GetType(),
            t => t.GetProperty(nameof(Results.Result.IsFailure)));
        return prop?.GetValue(response) is true;
    }
}
