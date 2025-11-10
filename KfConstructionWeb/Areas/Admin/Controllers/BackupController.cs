using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Models.ViewModels;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can backup/restore
public class BackupController : Controller
{
    private readonly IBackupRestoreService _backupService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<BackupController> _logger;

    public BackupController(
        IBackupRestoreService backupService,
        UserManager<IdentityUser> userManager,
        ILogger<BackupController> logger)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Display backup/restore management page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var model = new BackupRestoreViewModel
            {
                AvailableBackups = await _backupService.GetAvailableBackupsAsync(),
                DatabaseSizeBytes = await _backupService.GetDatabaseSizeBytesAsync(),
                LastBackupDate = (await _backupService.GetAvailableBackupsAsync())
                    .OrderByDescending(b => b.CreatedDate)
                    .FirstOrDefault()?.CreatedDate
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading backup page");
            TempData["Error"] = "An error occurred while loading backup information.";
            return View(new BackupRestoreViewModel());
        }
    }

    /// <summary>
    /// Create a new database backup
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBackup()
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var performedBy = currentUser?.Email ?? "System";

            var (success, backupPath, errorMessage) = await _backupService.CreateBackupAsync(performedBy);

            if (success)
            {
                TempData["Success"] = $"Database backup created successfully! File: {Path.GetFileName(backupPath)}";
            }
            else
            {
                TempData["Error"] = errorMessage;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup");
            TempData["Error"] = "An error occurred while creating the backup.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Restore database from backup
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                TempData["Error"] = "Backup file name is required.";
                return RedirectToAction(nameof(Index));
            }

            var backups = await _backupService.GetAvailableBackupsAsync();
            var backup = backups.FirstOrDefault(b => b.FileName == fileName);

            if (backup == null)
            {
                TempData["Error"] = "Backup file not found.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var performedBy = currentUser?.Email ?? "System";

            var (success, errorMessage) = await _backupService.RestoreDatabaseAsync(backup.FilePath, performedBy);

            if (success)
            {
                TempData["Success"] = $"Database restored successfully from: {fileName}";
            }
            else
            {
                TempData["Error"] = errorMessage;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database");
            TempData["Error"] = "An error occurred while restoring the database.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Download backup file
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Download(string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return NotFound();
            }

            var (success, fileStream, downloadFileName, errorMessage) = await _backupService.DownloadBackupAsync(fileName);

            if (success && fileStream != null)
            {
                return File(fileStream, "application/octet-stream", downloadFileName);
            }
            else
            {
                TempData["Error"] = errorMessage;
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading backup");
            TempData["Error"] = "An error occurred while downloading the backup.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Delete backup file
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                TempData["Error"] = "Backup file name is required.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _backupService.DeleteBackupAsync(fileName);

            if (success)
            {
                TempData["Success"] = $"Backup file '{fileName}' deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete backup file.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup");
            TempData["Error"] = "An error occurred while deleting the backup.";
            return RedirectToAction(nameof(Index));
        }
    }
}
