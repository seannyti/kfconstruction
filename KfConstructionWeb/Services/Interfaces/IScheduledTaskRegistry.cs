using KfConstructionWeb.Models.Tasks;

namespace KfConstructionWeb.Services.Interfaces;

public interface IScheduledTaskRegistry
{
    void ReportScheduled(string name, string description, DateTime nextRunUtc);
    void ReportStart(string name, string description, DateTime? nextRunUtc = null);
    void ReportSuccess(string name, TimeSpan duration, string? message, DateTime nextRunUtc);
    void ReportFailure(string name, TimeSpan duration, string? message, DateTime nextRunUtc);
    IReadOnlyCollection<ScheduledTaskInfo> GetAll();
    ScheduledTaskInfo? Get(string name);
}
