using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Platform.Integration.Policies;

/// <summary>
/// HTTP client resilience policies for external services.
/// </summary>
public static class HttpPolicies
{
    /// <summary>
    /// Retry policy: 3 attempts with exponential backoff.
    /// Retries on transient errors and 5xx responses.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => (int)msg.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),  // 2s, 4s, 8s
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    var logger = context.TryGetValue("logger", out var l) ? l as ILogger : null;
                    logger?.LogWarning(
                        "HTTP retry {RetryCount} after {Delay}s. Status: {Status}",
                        retryCount, timeSpan.TotalSeconds, outcome.Result?.StatusCode);
                });
    }

    /// <summary>
    /// Circuit breaker: Opens after 5 consecutive failures, stays open for 30s.
    /// Prevents cascade failures when external service is down.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    // Circuit opened — external service is considered down
                },
                onReset: () =>
                {
                    // Circuit closed — external service recovered
                });
    }

    /// <summary>
    /// Timeout policy: Fails after 30 seconds.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Combined policy wrap: Timeout → Circuit Breaker → Retry
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy.WrapAsync(
            GetTimeoutPolicy(),
            GetCircuitBreakerPolicy(),
            GetRetryPolicy());
    }
}
