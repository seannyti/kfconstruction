using KfConstructionWeb.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KfConstructionWeb.ViewComponents;

public class BroadcastBellViewComponent : ViewComponent
{
    private readonly IBroadcastMessageService _broadcastService;
    private readonly UserManager<IdentityUser> _userManager;

    public BroadcastBellViewComponent(
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
            return View("Default", new BroadcastBellViewModel());
        }

        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return View("Default", new BroadcastBellViewModel());
        }

        var unread = await _broadcastService.GetUnreadMessagesForUserAsync(user.Id);
        var top = unread.Take(5).ToList();

        var model = new BroadcastBellViewModel
        {
            UnreadCount = unread.Count(),
            UnreadTop = top
        };

        return View("Default", model);
    }
}

public class BroadcastBellViewModel
{
    public int UnreadCount { get; set; }
    public List<KfConstructionWeb.Models.BroadcastMessage> UnreadTop { get; set; } = new();
}
