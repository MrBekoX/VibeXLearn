using System.Net;
using Platform.Application.Common.Results;

namespace Platform.Integration.Common;

/// <summary>
/// Maps external API HTTP errors to standardized application errors.
/// SKILL: use-external-api-error-mapper
/// </summary>
public static class ExternalApiErrorMapper
{
    /// <summary>
    /// Maps HTTP status code to standardized error tuple.
    /// </summary>
    /// <param name="status">The HTTP status code</param>
    /// <param name="serviceName">Optional service name prefix (e.g., "GITHUB", "IYZICO")</param>
    /// <param name="retryAfter">Optional retry-after header value</param>
    /// <returns>Tuple of (ErrorCode, ErrorMessage)</returns>
    public static (string Code, string Message) Map(
        HttpStatusCode status,
        string? serviceName = null,
        string? retryAfter = null)
    {
        var prefix = string.IsNullOrEmpty(serviceName) ? "EXT" : serviceName.ToUpperInvariant();

        return status switch
        {
            HttpStatusCode.Unauthorized =>
                ($"{prefix}_UNAUTHORIZED", "External service: unauthorized. Check API credentials."),

            HttpStatusCode.Forbidden =>
                ($"{prefix}_FORBIDDEN", "External service: forbidden. Insufficient permissions."),

            HttpStatusCode.NotFound =>
                ($"{prefix}_NOT_FOUND", "Resource not found on external service."),

            HttpStatusCode.TooManyRequests =>
                ($"{prefix}_RATE_LIMITED",
                    retryAfter is not null
                    ? $"Rate limited. Retry after: {retryAfter}"
                    : "Rate limited. Please try again later."),

            HttpStatusCode.BadRequest =>
                ($"{prefix}_BAD_REQUEST", "Invalid request to external service."),

            HttpStatusCode.BadGateway =>
                ($"{prefix}_BAD_GATEWAY", "External service is unreachable."),

            HttpStatusCode.ServiceUnavailable =>
                ($"{prefix}_UNAVAILABLE", "External service temporarily unavailable."),

            HttpStatusCode.GatewayTimeout =>
                ($"{prefix}_TIMEOUT", "External service request timed out."),

            HttpStatusCode.Conflict =>
                ($"{prefix}_CONFLICT", "Resource conflict on external service."),

            HttpStatusCode.RequestEntityTooLarge =>
                ($"{prefix}_PAYLOAD_TOO_LARGE", "Request payload too large for external service."),

            >= HttpStatusCode.InternalServerError =>
                ($"{prefix}_SERVER_ERROR", "External service encountered an error."),

            _ => ($"{prefix}_UNKNOWN", $"External service error: {(int)status}")
        };
    }

    /// <summary>
    /// Determines if the error is retryable.
    /// </summary>
    public static bool IsRetryable(HttpStatusCode status)
    {
        return status switch
        {
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets recommended retry delay from headers or default.
    /// </summary>
    public static TimeSpan? GetRetryDelay(HttpResponseMessage? response)
    {
        if (response is null) return null;

        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta;

        if (response.Headers.RetryAfter?.Date is { } date)
            return date - DateTimeOffset.UtcNow;

        // Default delays based on status
        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => TimeSpan.FromMinutes(1),
            HttpStatusCode.ServiceUnavailable => TimeSpan.FromSeconds(30),
            HttpStatusCode.GatewayTimeout => TimeSpan.FromSeconds(10),
            HttpStatusCode.InternalServerError => TimeSpan.FromSeconds(5),
            _ => null
        };
    }

    /// <summary>
    /// Creates a Result.Fail from HTTP error.
    /// </summary>
    public static Result<T> ToFailedResult<T>(
        HttpStatusCode status,
        string? serviceName = null,
        string? retryAfter = null)
    {
        var (code, message) = Map(status, serviceName, retryAfter);
        return Result.Fail<T>(code, message);
    }
}
