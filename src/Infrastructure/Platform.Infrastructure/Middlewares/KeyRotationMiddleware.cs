using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Platform.Infrastructure.Middlewares;

/// <summary>
/// Adds key rotation headers to responses for client awareness.
/// SKILL: jwt-asymmetric-keys
/// </summary>
public class KeyRotationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<KeyRotationMiddleware> _logger;

    public KeyRotationMiddleware(
        RequestDelegate next,
        ILogger<KeyRotationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add key rotation headers to all responses
        context.Response.OnStarting(() =>
        {
            // Indicate that key rotation is supported
            context.Response.Headers["X-Key-Rotation-Supported"] = "true";

            // Try to get current key ID from KeyRotationService if available
            try
            {
                var keyRotationService = context.RequestServices
                    .GetService<Services.KeyRotationService>();

                if (keyRotationService is not null)
                {
                    context.Response.Headers["X-Current-Key-Id"] = keyRotationService.CurrentKeyId;
                }
            }
            catch
            {
                // Service may not be available in all contexts
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
