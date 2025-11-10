namespace KfConstructionWeb.Models.Tasks;

public class ScheduledTaskInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? LastStartUtc { get; set; }
    public DateTime? LastEndUtc { get; set; }
    public bool? LastSucceeded { get; set; }
    public string? LastMessage { get; set; }
    public TimeSpan? LastDuration { get; set; }
    public DateTime? NextRunUtc { get; set; }
    public DateTime FirstObservedUtc { get; set; } = DateTime.UtcNow;
}
