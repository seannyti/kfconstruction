using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionWeb.ViewComponents;

public class BroadcastBannerViewComponent : ViewComponent
{
    private readonly IBroadcastMessageService _broadcastService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public BroadcastBannerViewComponent(
        IBroadcastMessageService broadcastService,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext dbContext)
    {
        _broadcastService = broadcastService;
        _userManager = userManager;
        _dbContext = dbContext;
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

        // Get user's display name
        var userEmail = User.Identity?.Name;
        var client = userEmail != null 
            ? await _dbContext.Clients.FirstOrDefaultAsync(c => c.Email == userEmail) 
            : null;
        var displayName = client != null ? $"{client.FirstName} {client.LastName}" : (userEmail ?? "User");

        // Check for unread broadcasts
        var unread = (await _broadcastService.GetUnreadMessagesForUserAsync(user.Id)).ToList();
        
        var viewModel = new BroadcastBannerViewModel
        {
            UserDisplayName = displayName ?? "User",
            HasBroadcast = unread.Any(),
            BroadcastMessage = unread.Any() ? unread.OrderByDescending(m => m.SentAt).First() : null
        };

        return View("Default", viewModel);
    }
}

public class BroadcastBannerViewModel
{
    public string UserDisplayName { get; set; } = string.Empty;
    public bool HasBroadcast { get; set; }
    public BroadcastMessage? BroadcastMessage { get; set; }
}
