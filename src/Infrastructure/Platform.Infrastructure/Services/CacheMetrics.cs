using System.Diagnostics.Metrics;

namespace Platform.Infrastructure.Services;

/// <summary>
/// Cache observability counters using System.Diagnostics.Metrics.
/// Singleton — one Meter per application instance.
/// </summary>
public sealed class CacheMetrics : IDisposable
{
    public const string MeterName = "VibeXLearn.Cache";

    private readonly Meter _meter;

    private readonly Counter<long> _l1Hit;
    private readonly Counter<long> _l1Miss;
    private readonly Counter<long> _l2Hit;
    private readonly Counter<long> _l2Miss;
    private readonly Counter<long> _invalidation;
    private readonly Counter<long> _lockAcquired;
    private readonly Counter<long> _lockTimeout;

    public CacheMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        _l1Hit        = _meter.CreateCounter<long>("cache.l1.hit",        description: "L1 (memory) cache hits");
        _l1Miss       = _meter.CreateCounter<long>("cache.l1.miss",       description: "L1 (memory) cache misses");
        _l2Hit        = _meter.CreateCounter<long>("cache.l2.hit",        description: "L2 (Redis) cache hits");
        _l2Miss       = _meter.CreateCounter<long>("cache.l2.miss",       description: "L2 (Redis) cache misses");
        _invalidation = _meter.CreateCounter<long>("cache.invalidation",  description: "Cache invalidation operations");
        _lockAcquired = _meter.CreateCounter<long>("cache.lock.acquired", description: "Cache lock acquisitions");
        _lockTimeout  = _meter.CreateCounter<long>("cache.lock.timeout",  description: "Cache lock acquisition timeouts");
    }

    private static string ExtractPrefix(string key)
    {
        var firstColon = key.IndexOf(':');
        return firstColon > 0 ? key[..firstColon] : key;
    }

    public void RecordL1Hit(string key)
        => _l1Hit.Add(1, new KeyValuePair<string, object?>("key_prefix", ExtractPrefix(key)));

    public void RecordL1Miss(string key)
        => _l1Miss.Add(1, new KeyValuePair<string, object?>("key_prefix", ExtractPrefix(key)));

    public void RecordL2Hit(string key)
        => _l2Hit.Add(1, new KeyValuePair<string, object?>("key_prefix", ExtractPrefix(key)));

    public void RecordL2Miss(string key)
        => _l2Miss.Add(1, new KeyValuePair<string, object?>("key_prefix", ExtractPrefix(key)));

    public void RecordInvalidation(string pattern, string strategy)
        => _invalidation.Add(1,
            new KeyValuePair<string, object?>("pattern", pattern),
            new KeyValuePair<string, object?>("strategy", strategy));

    public void RecordLockAcquired(string key)
        => _lockAcquired.Add(1, new KeyValuePair<string, object?>("key_prefix", ExtractPrefix(key)));

    public void RecordLockTimeout(string key)
        => _lockTimeout.Add(1, new KeyValuePair<string, object?>("key_prefix", ExtractPrefix(key)));

    public void Dispose() => _meter.Dispose();
}
