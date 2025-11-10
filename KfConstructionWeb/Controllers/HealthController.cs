using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Controllers;

/// <summary>
/// Health check and performance monitoring endpoints
/// For 99.9% SLO monitoring and alerting
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IPerformanceTracker _performanceTracker;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        IPerformanceTracker performanceTracker,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _performanceTracker = performanceTracker ?? throw new ArgumentNullException(nameof(performanceTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Basic health check endpoint (public for monitoring systems)
    /// </summary>
    /// <returns>200 OK if healthy, 503 if unhealthy</returns>
    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult Liveness()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness check - ensures all dependencies are available
    /// </summary>
    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> Readiness()
    {
        var health = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = health.Status.ToString(),
            checks = health.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data
            }),
            timestamp = DateTime.UtcNow
        };

        return health.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }

    /// <summary>
    /// Detailed health report (admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetHealth()
    {
        var health = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = health.Status.ToString(),
            totalDuration = health.TotalDuration,
            checks = health.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            }),
            timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Get performance metrics (admin only)
    /// </summary>
    [HttpGet("metrics")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult GetMetrics()
    {
        var metrics = _performanceTracker.GetAllMetrics();

        var response = new
        {
            metrics = metrics.Select(m => new
            {
                operation = m.Key,
                totalRequests = m.Value.TotalRequests,
                averageLatencyMs = Math.Round(m.Value.AverageLatencyMs, 2),
                p50LatencyMs = m.Value.P50LatencyMs,
                p95LatencyMs = m.Value.P95LatencyMs,
                p99LatencyMs = m.Value.P99LatencyMs,
                minLatencyMs = m.Value.MinLatencyMs,
                maxLatencyMs = m.Value.MaxLatencyMs,
                p95Target = 200, // Target from requirements
                p95Met = m.Value.P95LatencyMs <= 200,
                lastUpdated = m.Value.LastUpdated
            }),
            timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Reset performance metrics (admin only)
    /// </summary>
    [HttpPost("metrics/reset")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult ResetMetrics()
    {
        _performanceTracker.Reset();
        _logger.LogInformation("Performance metrics reset by {User}", User.Identity?.Name);
        return Ok(new { message = "Metrics reset successfully", timestamp = DateTime.UtcNow });
    }
}
