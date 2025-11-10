using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KfConstructionWeb.ViewComponents;

public class BroadcastBannerViewComponent : ViewComponent
{
    private readonly IBroadcastMessageService _broadcastService;
    private readonly UserManager<IdentityUser> _userManager;

    public BroadcastBannerViewComponent(
        IBroadcastMessageService broadcastService,
        UserManager<IdentityUser> userManager)
    {
        _broadcastService = broadcastService;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Content(string.Empty);
        }

        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Content(string.Empty);
        }

        var unread = (await _broadcastService.GetUnreadMessagesForUserAsync(user.Id)).ToList();
        if (!unread.Any())
        {
            return Content(string.Empty);
        }

        // Show the most recent one
        var latest = unread.OrderByDescending(m => m.SentAt).First();
        return View("Default", latest);
    }
}
