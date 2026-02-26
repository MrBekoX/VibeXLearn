using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Validates request size early in the pipeline.
/// SKILL: request-size-limits
/// </summary>
public class RequestSizeValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSizeValidationMiddleware> _logger;
    private readonly long _maxRequestSize;

    public RequestSizeValidationMiddleware(
        RequestDelegate next,
        ILogger<RequestSizeValidationMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _maxRequestSize = config.GetValue("MaxRequestSize", 10 * 1024 * 1024L); // 10MB default
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for GET, HEAD, OPTIONS
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check content length
        if (context.Request.ContentLength.HasValue &&
            context.Request.ContentLength > _maxRequestSize)
        {
            _logger.LogWarning(
                "Request rejected: body too large. Size: {Size}, Max: {Max}, Path: {Path}",
                context.Request.ContentLength, _maxRequestSize, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Request body too large",
                maxSize = $"{_maxRequestSize / (1024 * 1024)}MB"
            });
            return;
        }

        await _next(context);
    }
}
