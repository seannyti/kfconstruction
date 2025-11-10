using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Areas.Admin.Controllers;

/// <summary>
/// Admin monitoring dashboard for system health and performance
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class MonitoringController : Controller
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IPerformanceTracker _performanceTracker;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        HealthCheckService healthCheckService,
        IPerformanceTracker performanceTracker,
        ILogger<MonitoringController> logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _performanceTracker = performanceTracker ?? throw new ArgumentNullException(nameof(performanceTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Display monitoring dashboard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var health = await _healthCheckService.CheckHealthAsync();
        var metrics = _performanceTracker.GetAllMetrics();

        ViewBag.HealthStatus = health.Status.ToString();
        ViewBag.HealthChecks = health.Entries;
        ViewBag.PerformanceMetrics = metrics;

        return View();
    }
}
