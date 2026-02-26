using MediatR;
using Microsoft.Extensions.Hosting;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Features.Badges.Queries.GetAllBadges;
using Platform.Application.Features.Categories.Queries.GetCategoryTree;
using Platform.Application.Features.Courses.Queries.GetAllCourses;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Warms critical cache keys on application startup to avoid cold-start latency.
/// Sends queries through MediatR so they pass the full pipeline (including QueryCachingBehavior).
/// Errors are logged but never prevent application startup.
/// </summary>
public sealed class CacheWarmupService(
    IServiceScopeFactory scopeFactory,
    ILogger<CacheWarmupService> logger) : BackgroundService
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Allow the application to fully start before warming cache
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);

        logger.LogInformation("Cache warmup starting...");

        var tasks = new (string Name, Func<IMediator, CancellationToken, Task> Action)[]
        {
            ("CategoryTree", static (m, ct) => m.Send(new GetCategoryTreeQuery(), ct)),
            ("CoursesPage1", static (m, ct) => m.Send(new GetAllCoursesQuery(new PageRequest { Page = 1, PageSize = 20 }), ct)),
            ("BadgesPage1",  static (m, ct) => m.Send(new GetAllBadgesQuery(new PageRequest { Page = 1, PageSize = 20 }), ct)),
        };

        foreach (var (name, action) in tasks)
        {
            if (stoppingToken.IsCancellationRequested) break;
            await WarmWithRetryAsync(name, action, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("Cache warmup completed.");
    }

    private async Task WarmWithRetryAsync(
        string name,
        Func<IMediator, CancellationToken, Task> action,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await action(mediator, ct).ConfigureAwait(false);
                logger.LogInformation("Cache warmup: {Name} succeeded (attempt {Attempt}).", name, attempt);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex,
                    "Cache warmup: {Name} failed (attempt {Attempt}/{Max}).",
                    name, attempt, MaxRetries);

                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelay, ct).ConfigureAwait(false);
            }
        }

        logger.LogError("Cache warmup: {Name} failed after {Max} attempts. Skipping.", name, MaxRetries);
    }
}
