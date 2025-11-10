using KfConstructionWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class TasksController : Controller
{
    private readonly IScheduledTaskRegistry _registry;
    private readonly IConfiguration _config;

    public TasksController(IScheduledTaskRegistry registry, IConfiguration config)
    {
        _registry = registry;
        _config = config;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var tasks = _registry.GetAll().ToList();

        // Include configured but not-yet-implemented tasks for visibility
        var backupVerifyEnabled = _config.GetValue<bool>("ReceiptSettings:EnableBackupVerification");
        var backupVerifyCron = _config["ReceiptSettings:BackupVerificationSchedule"];
        if (!tasks.Any(t => t.Name == "BackupVerification"))
        {
            tasks.Add(new Models.Tasks.ScheduledTaskInfo
            {
                Name = "BackupVerification",
                Description = "Verifies backups according to configured schedule",
                LastMessage = backupVerifyEnabled ? "Enabled (service not implemented)" : "Disabled",
                NextRunUtc = null
            });
        }

        ViewBag.BackupVerificationSchedule = backupVerifyCron;
        return View(tasks);
    }
}
