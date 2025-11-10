using System.Collections.Concurrent;
using KfConstructionWeb.Models.Performance;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

/// <summary>
/// Implementation of performance tracking service
/// Thread-safe performance monitoring for OWASP ASVS L2 compliance
/// </summary>
public class PerformanceTracker : IPerformanceTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _latencies = new();
    private readonly ILogger<PerformanceTracker> _logger;
    private readonly int _maxSamplesPerOperation = 10000; // Keep last 10k samples

    public PerformanceTracker(ILogger<PerformanceTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void TrackLatency(string operation, long milliseconds)
    {
        var bag = _latencies.GetOrAdd(operation, _ => new ConcurrentBag<long>());
        bag.Add(milliseconds);

        // Keep only the most recent samples to prevent memory issues
        if (bag.Count > _maxSamplesPerOperation)
        {
            // Remove oldest samples (approximate, good enough for monitoring)
            var samples = bag.ToArray();
            var recent = samples.Skip(samples.Length - (_maxSamplesPerOperation / 2)).ToArray();
            _latencies[operation] = new ConcurrentBag<long>(recent);
        }

        // Log if exceeds target
        if (milliseconds > 200)
        {
            _logger.LogWarning(
                "Operation '{Operation}' exceeded p95 target: {Latency}ms",
                operation, milliseconds);
        }
    }

    public PerformanceMetrics GetMetrics(string operation)
    {
        if (!_latencies.TryGetValue(operation, out var bag) || bag.IsEmpty)
        {
            return new PerformanceMetrics
            {
                Operation = operation,
                LastUpdated = DateTime.UtcNow
            };
        }

        var samples = bag.ToArray();
        Array.Sort(samples);

        return new PerformanceMetrics
        {
            Operation = operation,
            TotalRequests = samples.Length,
            AverageLatencyMs = samples.Average(),
            P50LatencyMs = GetPercentile(samples, 50),
            P95LatencyMs = GetPercentile(samples, 95),
            P99LatencyMs = GetPercentile(samples, 99),
            MinLatencyMs = samples.Min(),
            MaxLatencyMs = samples.Max(),
            LastUpdated = DateTime.UtcNow
        };
    }

    public Dictionary<string, PerformanceMetrics> GetAllMetrics()
    {
        return _latencies.Keys.ToDictionary(
            operation => operation,
            operation => GetMetrics(operation));
    }

    public void Reset()
    {
        _latencies.Clear();
        _logger.LogInformation("Performance metrics reset");
    }

    private static long GetPercentile(long[] sortedSamples, int percentile)
    {
        if (sortedSamples.Length == 0)
            return 0;

        var index = (int)Math.Ceiling(percentile / 100.0 * sortedSamples.Length) - 1;
        index = Math.Max(0, Math.Min(index, sortedSamples.Length - 1));
        return sortedSamples[index];
    }
}
