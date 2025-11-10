using KfConstructionWeb.Models.Performance;

namespace KfConstructionWeb.Services.Interfaces;

/// <summary>
/// Service for tracking p95 latency metrics
/// Thread-safe performance monitoring for OWASP ASVS L2 compliance
/// </summary>
public interface IPerformanceTracker
{
    void TrackLatency(string operation, long milliseconds);
    PerformanceMetrics GetMetrics(string operation);
    Dictionary<string, PerformanceMetrics> GetAllMetrics();
    void Reset();
}
