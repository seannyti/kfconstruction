using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using KfConstructionWeb.Data;
using KfConstructionWeb.Models;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class EmailLogsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailLogsController> _logger;

    public EmailLogsController(ApplicationDbContext context, ILogger<EmailLogsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Display email logs with filtering and pagination
    /// </summary>
    public async Task<IActionResult> Index(string? status = null, string? emailType = null, int page = 1, int pageSize = 20)
    {
        var query = _context.EmailLogs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrEmpty(emailType))
        {
            query = query.Where(e => e.EmailType == emailType);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering
        var emailLogs = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Calculate pagination info
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        // Get filter options for dropdowns
        var statuses = await _context.EmailLogs
            .Select(e => e.Status)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        var emailTypes = await _context.EmailLogs
            .Select(e => e.EmailType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // Pass data to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentEmailType = emailType;
        ViewBag.Statuses = statuses;
        ViewBag.EmailTypes = emailTypes;

        return View(emailLogs);
    }

    /// <summary>
    /// View email details
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var emailLog = await _context.EmailLogs.FindAsync(id);
        
        if (emailLog == null)
        {
            return NotFound();
        }

        return View(emailLog);
    }

    /// <summary>
    /// Delete email log
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var emailLog = await _context.EmailLogs.FindAsync(id);
            if (emailLog == null)
            {
                return NotFound();
            }

            _context.EmailLogs.Remove(emailLog);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Email log deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email log {EmailLogId}", id);
            TempData["ErrorMessage"] = "Error deleting email log.";
            return RedirectToAction(nameof(Index));
        }
    }
}