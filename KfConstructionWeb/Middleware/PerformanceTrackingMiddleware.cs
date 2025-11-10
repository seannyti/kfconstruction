using System.Diagnostics;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Middleware;

/// <summary>
/// Middleware for automatic latency tracking
/// </summary>
public class PerformanceTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceTrackingMiddleware> _logger;

    public PerformanceTrackingMiddleware(
        RequestDelegate next,
        ILogger<PerformanceTrackingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, IPerformanceTracker tracker)
    {
        // Only track specific paths (receipts)
        if (!context.Request.Path.StartsWithSegments("/Admin/Receipts"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var operation = $"{context.Request.Method} {context.Request.Path}";
            tracker.TrackLatency(operation, stopwatch.ElapsedMilliseconds);
        }
    }
}
