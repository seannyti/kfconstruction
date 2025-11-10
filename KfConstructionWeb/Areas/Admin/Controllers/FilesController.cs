using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.ViewModels;
using KfConstructionWeb.Services.Interfaces;
using KfConstructionWeb.Controllers;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class FilesController : BaseAdminController
{
    private readonly IFileManagementService _fileService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileManagementService fileService,
        UserManager<IdentityUser> userManager,
        ILogger<FilesController> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Display file browser with search and filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(FileSearchViewModel search)
    {
        try
        {
            var (files, totalCount) = await _fileService.SearchFilesAsync(
                search.SearchTerm,
                search.Category,
                search.UploadedAfter,
                search.UploadedBefore,
                search.UploadedBy,
                search.IsPublic,
                search.Page,
                search.PageSize);

            ViewBag.CurrentPage = search.Page;
            ViewBag.PageSize = search.PageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / search.PageSize);
            ViewBag.Categories = _fileService.GetCategories();
            ViewBag.SearchModel = search;

            return View(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading file list");
            SetErrorMessage("An error occurred while loading files.");
            return View(new List<UploadedFile>());
        }
    }

    /// <summary>
    /// Display upload form
    /// </summary>
    [HttpGet]
    public IActionResult Upload()
    {
        ViewBag.Categories = _fileService.GetCategories();
        return View(new FileUploadViewModel());
    }

    /// <summary>
    /// Handle file upload
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(FileUploadViewModel model)
    {
        if (!ModelState.IsValid || model.File == null)
        {
            ViewBag.Categories = _fileService.GetCategories();
            return View(model);
        }

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var uploadedBy = currentUser?.Email ?? "System";

            using var stream = model.File.OpenReadStream();
            var (success, fileId, errorMessage) = await _fileService.UploadFileAsync(
                stream,
                model.File.FileName,
                model.File.ContentType,
                model.File.Length,
                model.Category,
                model.Description,
                model.Tags,
                model.IsPublic,
                model.EncryptFile,
                uploadedBy);

            if (success)
            {
                SetSuccessMessage($"File '{model.File.FileName}' uploaded successfully!");
                return RedirectToAction(nameof(Details), new { id = fileId });
            }
            else
            {
                ModelState.AddModelError(string.Empty, errorMessage);
                ViewBag.Categories = _fileService.GetCategories();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            SetErrorMessage("An error occurred while uploading the file.");
            ViewBag.Categories = _fileService.GetCategories();
            return View(model);
        }
    }

    /// <summary>
    /// Display file details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var file = await _fileService.GetFileByIdAsync(id);
            if (file == null)
            {
                return NotFound();
            }

            return View(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading file details: ID={FileId}", id);
            SetErrorMessage("An error occurred while loading file details.");
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Download file
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var requestedBy = currentUser?.Email ?? "System";

            var (success, fileStream, contentType, fileName, errorMessage) = 
                await _fileService.DownloadFileAsync(id, requestedBy);

            if (success && fileStream != null)
            {
                return File(fileStream, contentType!, fileName!);
            }
            else
            {
                SetErrorMessage(errorMessage);
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: ID={FileId}", id);
            SetErrorMessage("An error occurred while downloading the file.");
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// Delete file (soft delete)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var deletedBy = currentUser?.Email ?? "System";

            var success = await _fileService.DeleteFileAsync(id, deletedBy);

            if (success)
            {
                SetSuccessMessage("File deleted successfully. It will be permanently removed after 30 days.");
            }
            else
            {
                SetErrorMessage("Failed to delete file.");
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: ID={FileId}", id);
            SetErrorMessage("An error occurred while deleting the file.");
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
