using System.Collections.Concurrent;
using KfConstructionWeb.Models.Tasks;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

public class ScheduledTaskRegistry : IScheduledTaskRegistry
{
    private readonly ConcurrentDictionary<string, ScheduledTaskInfo> _tasks = new();
    private readonly ILogger<ScheduledTaskRegistry> _logger;

    public ScheduledTaskRegistry(ILogger<ScheduledTaskRegistry> logger)
    {
        _logger = logger;
    }

    public void ReportScheduled(string name, string description, DateTime nextRunUtc)
    {
        var info = _tasks.GetOrAdd(name, _ => new ScheduledTaskInfo { Name = name, Description = description });
        info.NextRunUtc = nextRunUtc;
        info.LastMessage ??= "Scheduled";
        _logger.LogInformation("Task {Task} scheduled for {NextRun}", name, nextRunUtc);
    }

    public void ReportStart(string name, string description, DateTime? nextRunUtc = null)
    {
        var info = _tasks.GetOrAdd(name, _ => new ScheduledTaskInfo { Name = name, Description = description });
        info.Description = description;
        info.LastStartUtc = DateTime.UtcNow;
        if (nextRunUtc.HasValue) info.NextRunUtc = nextRunUtc.Value;
        info.LastMessage = "Running";
        _logger.LogInformation("Task {Task} started", name);
    }

    public void ReportSuccess(string name, TimeSpan duration, string? message, DateTime nextRunUtc)
    {
        var info = _tasks.GetOrAdd(name, _ => new ScheduledTaskInfo { Name = name });
        info.LastEndUtc = DateTime.UtcNow;
        info.LastDuration = duration;
        info.LastSucceeded = true;
        info.LastMessage = message ?? "Completed successfully";
        info.NextRunUtc = nextRunUtc;
        _logger.LogInformation("Task {Task} succeeded in {Duration}.", name, duration);
    }

    public void ReportFailure(string name, TimeSpan duration, string? message, DateTime nextRunUtc)
    {
        var info = _tasks.GetOrAdd(name, _ => new ScheduledTaskInfo { Name = name });
        info.LastEndUtc = DateTime.UtcNow;
        info.LastDuration = duration;
        info.LastSucceeded = false;
        info.LastMessage = message ?? "Failed";
        info.NextRunUtc = nextRunUtc;
        _logger.LogWarning("Task {Task} failed in {Duration}: {Message}", name, duration, message);
    }

    public IReadOnlyCollection<ScheduledTaskInfo> GetAll()
    {
        return _tasks.Values
            .OrderBy(t => t.Name)
            .ToList();
    }

    public ScheduledTaskInfo? Get(string name)
    {
        _tasks.TryGetValue(name, out var info);
        return info;
    }
}
