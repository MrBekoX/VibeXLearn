using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Platform.WebAPI.Extensions;

/// <summary>
/// Rate limiting extension'ları.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(opt =>
        {
            // Global sliding window
            opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    ctx.User.Identity?.Name ??
                    ctx.Connection.RemoteIpAddress?.ToString() ??
                    "anon",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit          = 100,
                        Window               = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow    = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    }));

            // Auth endpoint'leri için daha sıkı limit
            opt.AddPolicy("AuthPolicy", ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window      = TimeSpan.FromMinutes(15)
                    }));

            // Ödeme endpoint'leri için limit
            opt.AddPolicy("PaymentPolicy", ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    ctx.User.Identity?.Name ?? "anon",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit          = 5,
                        Window               = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow    = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    }));

            opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opt.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsJsonAsync(new
                {
                    Error = "Too many requests. Please try again later."
                }, ct);
            };
        });

        return services;
    }
}
