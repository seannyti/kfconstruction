namespace KfConstructionWeb.Models.Performance;

/// <summary>
/// Performance metrics for tracking request latency
/// </summary>
public class PerformanceMetrics
{
    public string Operation { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public double AverageLatencyMs { get; set; }
    public long P50LatencyMs { get; set; }
    public long P95LatencyMs { get; set; }
    public long P99LatencyMs { get; set; }
    public long MinLatencyMs { get; set; }
    public long MaxLatencyMs { get; set; }
    public DateTime LastUpdated { get; set; }
}
