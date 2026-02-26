---
name: use-external-api-error-mapper
description: ExternalApiErrorMapper var ama kullanılmıyor. External API hataları tutarsız şekilde handle ediliyor. Her service kendi ad-hoc error handling'ini yapıyor. Bu skill, standart error mapping kullanımını zorunlu kılar.
---

# Use ExternalApiErrorMapper in Services

## Problem

**Risk Level:** MEDIUM

`ExternalApiErrorMapper.Map()` HTTP status code'larını standart error code/message'a çeviren utility mevcut ama hiçbir service'de kullanılmıyor. Her service kendi ad-hoc error handling'ini yapıyor.

**Affected Files:**
- `src/Infrastructure/Platform.Integration/Common/ExternalApiErrorMapper.cs`
- `src/Infrastructure/Platform.Integration/Iyzico/IyzicoService.cs`
- `src/Infrastructure/Platform.Integration/GitHub/GitHubClient.cs`
- Other external service implementations

## Solution Steps

### Step 1: Verify ExternalApiErrorMapper Exists

Check: `src/Infrastructure/Platform.Integration/Common/ExternalApiErrorMapper.cs`

```csharp
namespace Platform.Integration.Common;

/// <summary>
/// Maps external API HTTP errors to standardized application errors.
/// </summary>
public static class ExternalApiErrorMapper
{
    /// <summary>
    /// Maps HTTP status code to standardized error.
    /// </summary>
    public static (string Code, string Message) Map(
        HttpStatusCode status,
        string? serviceName = null,
        string? retryAfter = null)
    {
        var prefix = string.IsNullOrEmpty(serviceName) ? "EXT" : serviceName.ToUpper();

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

            HttpStatusCode.BadGateway =>
                ($"{prefix}_BAD_GATEWAY", "External service is unreachable."),

            HttpStatusCode.ServiceUnavailable =>
                ($"{prefix}_UNAVAILABLE", "External service temporarily unavailable."),

            HttpStatusCode.GatewayTimeout =>
                ($"{prefix}_TIMEOUT", "External service request timed out."),

            >= HttpStatusCode.InternalServerError =>
                ($"{prefix}_SERVER_ERROR", "External service encountered an error."),

            HttpStatusCode.BadRequest =>
                ($"{prefix}_BAD_REQUEST", "Invalid request to external service."),

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
            return date.Offset - DateTimeOffset.UtcNow;

        // Default delays based on status
        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => TimeSpan.FromMinutes(1),
            HttpStatusCode.ServiceUnavailable => TimeSpan.FromSeconds(30),
            HttpStatusCode.GatewayTimeout => TimeSpan.FromSeconds(10),
            _ => null
        };
    }
}
```

### Step 2: Update IyzicoService to Use Mapper

```csharp
using System.Net;
using Platform.Integration.Common;

public async Task<Result<CheckoutInitResponse>> InitiateCheckoutAsync(
    string conversationId,
    IyzicoBuyerInfo buyer,
    string courseTitle,
    decimal price,
    CancellationToken ct = default)
{
    try
    {
        // ... build request ...

        var checkoutForm = await CheckoutFormInitialize.Create(request, GetOptions());

        if (checkoutForm.Status == Status.SUCCESS.ToString())
        {
            return Result<CheckoutInitResponse>.Success(
                new CheckoutInitResponse(checkoutForm.Token!, checkoutForm.CheckoutFormContent!));
        }

        // Map Iyzico-specific errors
        var (code, message) = checkoutForm.ErrorCode switch
        {
            "NOT_ENOUGH_BALANCE" => ("IYZICO_INSUFFICIENT_BALANCE", "Insufficient balance"),
            "CARD_NOT_SUPPORTED" => ("IYZICO_CARD_NOT_SUPPORTED", "Card not supported"),
            "INVALID_CARD_NUMBER" => ("IYZICO_INVALID_CARD", "Invalid card number"),
            _ => ("IYZICO_FAILED", checkoutForm.ErrorMessage ?? "Payment failed")
        };

        _logger.LogWarning(
            "{Event} | ConvId:{ConvId} | Code:{Code} | Message:{Message}",
            "IYZICO_CHECKOUT_FAILED", conversationId, code, message);

        return Result<CheckoutInitResponse>.Fail(code, message);
    }
    catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
    {
        // USE EXTERNAL API ERROR MAPPER
        var (code, message) = ExternalApiErrorMapper.Map(
            ex.StatusCode.Value,
            serviceName: "IYZICO");

        _logger.LogWarning(ex,
            "{Event} | ConvId:{ConvId} | Code:{Code}",
            "IYZICO_HTTP_ERROR", conversationId, code);

        return Result<CheckoutInitResponse>.Fail(code, message);
    }
    catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
    {
        // Timeout
        _logger.LogWarning(ex,
            "{Event} | ConvId:{ConvId}",
            "IYZICO_TIMEOUT", conversationId);

        return Result<CheckoutInitResponse>.Fail(
            "IYZICO_TIMEOUT",
            "Payment service request timed out");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "{Event} | ConvId:{ConvId}",
            "IYZICO_EXCEPTION", conversationId);

        return Result<CheckoutInitResponse>.Fail(
            "IYZICO_EXCEPTION",
            "Payment service unavailable");
    }
}
```

### Step 3: Update GitHubClient to Use Mapper

```csharp
using System.Net;
using Platform.Integration.Common;

public async Task<Result<GitHubRepo>> GetRepositoryAsync(
    string owner,
    string repo,
    CancellationToken ct = default)
{
    try
    {
        var response = await _httpClient.GetAsync(
            $"repos/{owner}/{repo}",
            ct);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var repository = JsonSerializer.Deserialize<GitHubRepo>(content);

        return Result.Success(repository!);
    }
    catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
    {
        var statusCode = ex.StatusCode.Value;

        // Check rate limiting
        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = ExternalApiErrorMapper.GetRetryDelay(null);
            _logger.LogWarning(
                "GitHub rate limited. Retry after: {RetryAfter}",
                retryAfter);
        }

        // USE EXTERNAL API ERROR MAPPER
        var (code, message) = ExternalApiErrorMapper.Map(
            statusCode,
            serviceName: "GITHUB");

        _logger.LogWarning(
            "GitHub API error: {Code} - {Message}",
            code, message);

        return Result.Fail<GitHubRepo>(code, message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "GitHub API request failed");
        return Result.Fail<GitHubRepo>(
            "GITHUB_EXCEPTION",
            "GitHub service unavailable");
    }
}
```

### Step 4: Create Base External Service Class

Create: `src/Infrastructure/Platform.Integration/Common/ExternalServiceBase.cs`

```csharp
using System.Net;
using Microsoft.Extensions.Logging;
using Platform.Application.Common.Results;
using Platform.Integration.Common;

namespace Platform.Integration.Common;

/// <summary>
/// Base class for external service implementations.
/// Provides standardized error handling and logging.
/// </summary>
public abstract class ExternalServiceBase
{
    protected abstract string ServiceName { get; }

    protected Result<T> HandleHttpError<T>(
        HttpRequestException ex,
        string operation,
        ILogger logger,
        HttpResponseMessage? response = null)
    {
        var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
        var (code, message) = ExternalApiErrorMapper.Map(statusCode, ServiceName);

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
}
```

### Step 5: Use Base Class in Implementations

```csharp
public class GitHubClient : ExternalServiceBase, IGitHubClient
{
    protected override string ServiceName => "GITHUB";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubClient> _logger;

    public GitHubClient(HttpClient httpClient, ILogger<GitHubClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<GitHubRepo>> GetRepositoryAsync(
        string owner,
        string repo,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"repos/{owner}/{repo}", ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var repository = JsonSerializer.Deserialize<GitHubRepo>(content);

            return Result.Success(repository!);
        }
        catch (HttpRequestException ex)
        {
            return HandleHttpError<GitHubRepo>(ex, "GetRepository", _logger);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return HandleTimeout<GitHubRepo>("GetRepository", _logger);
        }
        catch (Exception ex)
        {
            return HandleException<GitHubRepo>(ex, "GetRepository", _logger);
        }
    }
}
```

## Verification

```bash
# Test with simulated API error
# Force a 401 response
curl -X POST http://localhost:8080/api/v1/payments/checkout \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"courseId":"..."}'

# Response should have standardized error format:
# {
#   "code": "IYZICO_UNAUTHORIZED",
#   "message": "External service: unauthorized. Check API credentials."
# }
```

## Priority

**SHORT-TERM** - Code consistency and maintainability.
