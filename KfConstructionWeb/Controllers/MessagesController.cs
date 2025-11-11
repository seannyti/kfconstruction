using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Controllers;

/// <summary>
/// Public controller for users to interact with broadcast messages
/// </summary>
[Authorize]
public class MessagesController : Controller
{
    private readonly IBroadcastMessageService _broadcastService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IBroadcastMessageService broadcastService,
        UserManager<IdentityUser> userManager,
        ILogger<MessagesController> logger)
    {
        _broadcastService = broadcastService ?? throw new ArgumentNullException(nameof(broadcastService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Mark a broadcast message as read
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, string? returnUrl = null)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _broadcastService.MarkAsReadAsync(id, user.Id);
                _logger.LogInformation($"User {user.Email} marked message {id} as read");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", id);
            TempData["Error"] = "Failed to mark message as read.";
        }

        return RedirectToReturnUrl(returnUrl);
    }

    /// <summary>
    /// Dismiss a broadcast message (marks as read and dismissed)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id, string? returnUrl = null)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _broadcastService.DismissMessageAsync(id, user.Id);
                _logger.LogInformation($"User {user.Email} dismissed message {id}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing message {MessageId}", id);
            TempData["Error"] = "Failed to dismiss message.";
        }

        return RedirectToReturnUrl(returnUrl);
    }

    /// <summary>
    /// Helper to redirect to return URL or home
    /// </summary>
    private IActionResult RedirectToReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
