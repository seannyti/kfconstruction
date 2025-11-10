using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ActivityController : Controller
{
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(
        IActivityLogService activityLogService,
        ILogger<ActivityController> logger)
    {
        _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Display activity logs with filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? category,
        string? action,
        string? performedBy,
        DateTime? startDate,
        DateTime? endDate,
        string? severity,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            var (logs, totalCount) = await _activityLogService.GetActivityLogsAsync(
                category, action, performedBy, startDate, endDate, severity, page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Categories = ActivityCategories.GetAll();
            ViewBag.Severities = ActivitySeverity.GetAll();
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentAction = action;
            ViewBag.CurrentPerformedBy = performedBy;
            ViewBag.CurrentStartDate = startDate;
            ViewBag.CurrentEndDate = endDate;
            ViewBag.CurrentSeverity = severity;

            return View(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading activity logs");
            TempData["Error"] = "An error occurred while loading activity logs.";
            return View(new List<ActivityLog>());
        }
    }

    /// <summary>
    /// Show recent activity (dashboard widget)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Recent(int count = 50)
    {
        try
        {
            var logs = await _activityLogService.GetRecentActivityAsync(count);
            return View(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent activity");
            return View(new List<ActivityLog>());
        }
    }
}
