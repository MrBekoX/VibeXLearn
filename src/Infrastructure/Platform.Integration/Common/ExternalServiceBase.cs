using System.Net;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Results;

namespace Platform.Integration.Common;

/// <summary>
/// Base class for external service implementations.
/// Provides standardized error handling and logging.
/// SKILL: use-external-api-error-mapper
/// </summary>
public abstract class ExternalServiceBase
{
    /// <summary>
    /// The service name used in error codes and logging.
    /// </summary>
    protected abstract string ServiceName { get; }

    /// <summary>
    /// Handles HTTP errors using ExternalApiErrorMapper.
    /// </summary>
    protected Result<T> HandleHttpError<T>(
        HttpRequestException ex,
        string operation,
        ILogger logger,
        HttpResponseMessage? response = null)
    {
        var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
        var retryAfter = response?.Headers.RetryAfter?.ToString();
        var (code, message) = ExternalApiErrorMapper.Map(statusCode, ServiceName, retryAfter);

        // Check for rate limiting
        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            var retryDelay = ExternalApiErrorMapper.GetRetryDelay(response);
            logger.LogWarning(
                "{Service} rate limited during {Operation}. Retry after: {RetryDelay}",
                ServiceName, operation, retryDelay);
        }

        logger.LogWarning(ex,
            "{Service} error during {Operation}: {Code} - {Message}",
            ServiceName, operation, code, message);

        return Result.Fail<T>(code, message);
    }

    /// <summary>
    /// Handles timeout errors.
    /// </summary>
    protected Result<T> HandleTimeout<T>(
        string operation,
        ILogger logger)
    {
        logger.LogWarning(
            "{Service} timeout during {Operation}",
            ServiceName, operation);

        return Result.Fail<T>(
            $"{ServiceName}_TIMEOUT",
            $"{ServiceName} request timed out");
    }

    /// <summary>
    /// Handles unexpected exceptions.
    /// </summary>
    protected Result<T> HandleException<T>(
        Exception ex,
        string operation,
        ILogger logger)
    {
        logger.LogError(ex,
            "{Service} unexpected error during {Operation}",
            ServiceName, operation);

        return Result.Fail<T>(
            $"{ServiceName}_EXCEPTION",
            $"{ServiceName} service unavailable");
    }

    /// <summary>
    /// Creates a standardized error result.
    /// </summary>
    protected Result<T> CreateErrorResult<T>(string code, string message)
    {
        return Result.Fail<T>($"{ServiceName}_{code}", message);
    }
}
