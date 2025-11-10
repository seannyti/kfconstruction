namespace KfConstructionWeb.Services.Interfaces;

/// <summary>
/// Service for logging and retrieving activity/audit logs
/// </summary>
public interface IActivityLogService
{
    /// <summary>
    /// Log an activity
    /// </summary>
    Task LogActivityAsync(
        string category,
        string action,
        string performedBy,
        string? description = null,
        string? entityType = null,
        int? entityId = null,
        string severity = "Info",
        string? ipAddress = null,
        string? userAgent = null,
        object? additionalData = null);

    /// <summary>
    /// Get activity logs with filters
    /// </summary>
    Task<(List<Models.ActivityLog> Logs, int TotalCount)> GetActivityLogsAsync(
        string? category = null,
        string? action = null,
        string? performedBy = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get recent activity (last 100)
    /// </summary>
    Task<List<Models.ActivityLog>> GetRecentActivityAsync(int count = 100);

    /// <summary>
    /// Get activity for specific entity
    /// </summary>
    Task<List<Models.ActivityLog>> GetEntityActivityAsync(string entityType, int entityId);
}
