using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class BroadcastController : Controller
{
    private readonly IBroadcastMessageService _broadcastService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<BroadcastController> _logger;

    public BroadcastController(
        IBroadcastMessageService broadcastService,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<BroadcastController> logger)
    {
        _broadcastService = broadcastService ?? throw new ArgumentNullException(nameof(broadcastService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Broadcast messaging dashboard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var messages = await _broadcastService.GetAllMessagesAsync();
            var stats = await _broadcastService.GetStatisticsAsync();

            ViewBag.TotalSent = stats.TotalSent;
            ViewBag.Active = stats.Active;
            ViewBag.TotalRecipients = stats.TotalRecipients;
            ViewBag.TotalRead = stats.TotalRead;

            return View(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading broadcast dashboard");
            TempData["Error"] = "An error occurred while loading broadcast messages.";
            return View(new List<BroadcastMessage>());
        }
    }

    /// <summary>
    /// Create new broadcast message form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.MessageTypes = MessageTypes.GetAll();
        ViewBag.Roles = await GetAvailableRolesAsync();
        return View();
    }

    /// <summary>
    /// Send broadcast message
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string subject,
        string message,
        string messageType,
        string? targetRole,
        bool sendEmail,
        bool showInApp,
        DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            ModelState.AddModelError("", "Subject and message are required.");
            ViewBag.MessageTypes = MessageTypes.GetAll();
            ViewBag.Roles = await GetAvailableRolesAsync();
            return View();
        }

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var sentBy = currentUser?.Email ?? "System";

            var broadcastMessage = await _broadcastService.SendMessageAsync(
                subject,
                message,
                messageType,
                targetRole,
                sendEmail,
                showInApp,
                sentBy,
                expiresAt);

            TempData["Success"] = $"Broadcast message sent to {broadcastMessage.RecipientCount} users!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending broadcast message");
            ModelState.AddModelError("", "An error occurred while sending the broadcast message.");
            ViewBag.MessageTypes = MessageTypes.GetAll();
            ViewBag.Roles = await GetAvailableRolesAsync();
            return View();
        }
    }

    /// <summary>
    /// View message details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var message = await _broadcastService.GetMessageByIdAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        return View(message);
    }

    /// <summary>
    /// Deactivate a broadcast message
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            var success = await _broadcastService.DeactivateMessageAsync(id);
            if (success)
            {
                TempData["Success"] = "Broadcast message deactivated successfully.";
            }
            else
            {
                TempData["Error"] = "Message not found.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating broadcast message");
            TempData["Error"] = "An error occurred while deactivating the message.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Delete a broadcast message
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _broadcastService.DeleteMessageAsync(id);
            if (success)
            {
                TempData["Success"] = "Broadcast message deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Message not found.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting broadcast message");
            TempData["Error"] = "An error occurred while deleting the message.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _broadcastService.MarkAsReadAsync(id, user.Id);
                TempData["Success"] = "Message marked as read.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message read");
            TempData["Error"] = "Failed to mark message as read.";
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _broadcastService.DismissMessageAsync(id, user.Id);
                TempData["Success"] = "Message dismissed.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing message");
            TempData["Error"] = "Failed to dismiss message.";
        }
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Get available roles for targeting
    /// </summary>
    private async Task<List<string>> GetAvailableRolesAsync()
    {
        var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return roles.Where(r => r != null).Select(r => r!).ToList();
    }
}
