using System.Net;
using Platform.Domain.Common;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Global exception handling middleware.
/// Maps DomainException codes to appropriate HTTP status codes.
/// Stack trace is never sent to client.
/// </summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (DomainException ex)
        {
            var correlationId = ctx.TraceIdentifier;
            logger.LogWarning(ex,
                "Domain exception [{Code}]. CorrelationId: {CorrelationId}",
                ex.Code, correlationId);

            var statusCode = MapToStatusCode(ex.Code);

            ctx.Response.StatusCode  = statusCode;
            ctx.Response.ContentType = "application/json";

            await ctx.Response.WriteAsJsonAsync(new
            {
                Type          = ex.Code,
                Message       = ex.Message,
                CorrelationId = correlationId,
                Timestamp     = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            var correlationId = ctx.TraceIdentifier;
            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

            ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";

            await ctx.Response.WriteAsJsonAsync(new
            {
                Type          = "InternalServerError",
                Message       = "An unexpected error occurred.",
                CorrelationId = correlationId,
                Timestamp     = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Maps DomainException error codes to HTTP status codes by pattern matching.
    /// </summary>
    private static int MapToStatusCode(string code) => code switch
    {
        // Authentication / Authorization
        var c when c.StartsWith("AUTH", StringComparison.Ordinal)         => StatusCodes.Status401Unauthorized,
        var c when c.Contains("UNAUTHORIZED", StringComparison.Ordinal)  => StatusCodes.Status401Unauthorized,
        var c when c.Contains("FORBIDDEN", StringComparison.Ordinal)     => StatusCodes.Status403Forbidden,

        // Not found
        var c when c.Contains("NOT_FOUND", StringComparison.Ordinal)     => StatusCodes.Status404NotFound,

        // Conflicts (already exists / already applied / already revoked)
        var c when c.Contains("ALREADY_", StringComparison.Ordinal)      => StatusCodes.Status409Conflict,
        var c when c.Contains("CONFLICT", StringComparison.Ordinal)      => StatusCodes.Status409Conflict,

        // Business rule violations (invalid status transitions, expired, etc.)
        var c when c.Contains("INVALID", StringComparison.Ordinal)       => StatusCodes.Status422UnprocessableEntity,
        var c when c.Contains("EXPIRED", StringComparison.Ordinal)       => StatusCodes.Status422UnprocessableEntity,
        var c when c.Contains("CANNOT", StringComparison.Ordinal)        => StatusCodes.Status422UnprocessableEntity,

        // Validation errors
        var c when c.StartsWith("VALIDATION", StringComparison.Ordinal)  => StatusCodes.Status400BadRequest,

        // Default: treat as bad request
        _ => StatusCodes.Status400BadRequest
    };
}

