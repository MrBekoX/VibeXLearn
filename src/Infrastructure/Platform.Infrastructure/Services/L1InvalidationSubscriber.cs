using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Application.Common.Models.Cache;
using Platform.Infrastructure.Models;
using StackExchange.Redis;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Background service that subscribes to cache invalidation messages via Redis Pub/Sub.
/// Handles reconnection and self-invalidation guard.
/// </summary>
/// <remarks>
/// <para>
/// Self-invalidation guard: Instances ignore their own invalidation messages
/// since they've already invalidated their local L1 cache before broadcasting.
/// </para>
/// <para>
/// Reconnection handling: Automatically re-subscribes when Redis connection is restored.
/// </para>
/// </remarks>
public sealed class L1InvalidationSubscriber : IHostedService, IDisposable
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<L1InvalidationSubscriber> _logger;
    private readonly CacheSettings _settings;

    // Unique identifier for this instance - used for self-invalidation guard
    private readonly string _instanceId = Guid.NewGuid().ToString("N")[..8];

    // Tracked L1 keys for pattern matching (IMemoryCache doesn't support enumeration)
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    private ISubscriber? _subscriber;
    private bool _disposed;

    public L1InvalidationSubscriber(
        IConnectionMultiplexer multiplexer,
        IMemoryCache memoryCache,
        IOptions<CacheSettings> settings,
        ILogger<L1InvalidationSubscriber> logger)
    {
        _multiplexer = multiplexer;
        _memoryCache = memoryCache;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Instance ID for this subscriber (for debugging/logging).
    /// </summary>
    public string InstanceId => _instanceId;

    /// <summary>
    /// Registers a key for L1 tracking (used by CacheService for invalidation).
    /// </summary>
    public void TrackKey(string key)
    {
        _trackedKeys.TryAdd(key, 0);
    }

    /// <summary>
    /// Removes a key from L1 tracking.
    /// </summary>
    public void UntrackKey(string key)
    {
        _trackedKeys.TryRemove(key, out _);
    }

    /// <summary>
    /// Invalidates local L1 cache entries matching the pattern.
    /// </summary>
    public void InvalidateLocalL1(string pattern)
    {
        var regexPattern = ConvertGlobToRegex(pattern);
        var regex = new Regex(regexPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        var matchedCount = 0;
        foreach (var key in _trackedKeys.Keys.ToList())
        {
            if (regex.IsMatch(key))
            {
                _memoryCache.Remove(key);
                _trackedKeys.TryRemove(key, out _);
                matchedCount++;
            }
        }

        if (matchedCount > 0)
        {
            _logger.LogDebug(
                "L1 invalidation: {Pattern} matched {Count} keys",
                pattern, matchedCount);
        }
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_settings.EnableL1Synchronization)
        {
            _logger.LogInformation(
                "L1 synchronization disabled. Instance {InstanceId} will not subscribe to invalidation channel.",
                _instanceId);
            return;
        }

        // Subscribe to invalidation channel
        await SubscribeAsync(ct).ConfigureAwait(false);

        // Register reconnection handler
        _multiplexer.ConnectionRestored += OnConnectionRestored;
        _multiplexer.ConnectionFailed += OnConnectionFailed;

        _logger.LogInformation(
            "L1 invalidation subscriber started. Instance {InstanceId} listening on channel {Channel}",
            _instanceId, _settings.InvalidationChannelName);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _multiplexer.ConnectionRestored -= OnConnectionRestored;
        _multiplexer.ConnectionFailed -= OnConnectionFailed;

        if (_subscriber is not null)
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal(_settings.InvalidationChannelName))
                .ConfigureAwait(false);
        }

        _logger.LogInformation(
            "L1 invalidation subscriber stopped. Instance {InstanceId}",
            _instanceId);
    }

    private async Task SubscribeAsync(CancellationToken ct)
    {
        _subscriber = _multiplexer.GetSubscriber();
        await _subscriber.SubscribeAsync(
            RedisChannel.Literal(_settings.InvalidationChannelName),
            OnMessage
        ).ConfigureAwait(false);
    }

    private void OnMessage(RedisChannel channel, RedisValue message)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<CacheInvalidationMessage>((string)message!);
            if (msg is null)
            {
                _logger.LogWarning("Failed to deserialize invalidation message");
                return;
            }

            //  KRÍTÍK: Self-invalidation guard
            // Skip messages from this instance (already invalidated locally)
            if (msg.SourceInstance == _instanceId)
            {
                _logger.LogDebug(
                    "Ignoring self-invalidation: {Pattern}",
                    msg.KeyPattern);
                return;
            }

            _logger.LogDebug(
                "L1 invalidation received: {Pattern} from instance {Source}",
                msg.KeyPattern, msg.SourceInstance);

            // Invalidate matching L1 entries
            InvalidateLocalL1(msg.KeyPattern);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process cache invalidation message");
        }
    }

    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        _logger.LogInformation(
            "Redis connection restored. Instance {InstanceId} re-subscribing to {Channel}",
            _instanceId, _settings.InvalidationChannelName);

        // Fire-and-forget resubscription
        _ = Task.Run(async () =>
        {
            try
            {
                await SubscribeAsync(default).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-subscribe after connection restore");
            }
        });
    }

    private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        _logger.LogWarning(
            "Redis connection failed. Instance {InstanceId} may have stale L1 cache until reconnection. Exception: {ExceptionType} - {Message}",
            _instanceId, e.Exception?.GetType().Name ?? "Unknown", e.Exception?.Message ?? "No details");
    }

    private static string ConvertGlobToRegex(string glob)
    {
        // Convert glob pattern to regex
        // "courses:*" → "^courses:.*$"
        // "categories:tree" → "^categories:tree$"
        return "^" + Regex.Escape(glob).Replace("\\*", ".*") + "$";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _multiplexer.ConnectionRestored -= OnConnectionRestored;
        _multiplexer.ConnectionFailed -= OnConnectionFailed;
        _trackedKeys.Clear();
    }
}
