using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KfConstructionWeb.Services;

/// <summary>
/// Service for logging and retrieving activity/audit logs
/// </summary>
public class ActivityLogService : IActivityLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(
        ApplicationDbContext context,
        ILogger<ActivityLogService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogActivityAsync(
        string category,
        string action,
        string performedBy,
        string? description = null,
        string? entityType = null,
        int? entityId = null,
        string severity = "Info",
        string? ipAddress = null,
        string? userAgent = null,
        object? additionalData = null)
    {
        try
        {
            var log = new ActivityLog
            {
                Category = category,
                Action = action,
                PerformedBy = performedBy,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Severity = severity,
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Don't let logging failures crash the application
            _logger.LogError(ex, "Failed to log activity: {Category}/{Action}", category, action);
        }
    }

    public async Task<(List<ActivityLog> Logs, int TotalCount)> GetActivityLogsAsync(
        string? category = null,
        string? action = null,
        string? performedBy = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.ActivityLogs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(l => l.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(l => l.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(performedBy))
        {
            query = query.Where(l => l.PerformedBy.Contains(performedBy));
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query = query.Where(l => l.Severity == severity);
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<List<ActivityLog>> GetRecentActivityAsync(int count = 100)
    {
        return await _context.ActivityLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetEntityActivityAsync(string entityType, int entityId)
    {
        return await _context.ActivityLogs
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
    }
}
