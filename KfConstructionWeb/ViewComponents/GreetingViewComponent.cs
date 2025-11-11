using KfConstructionWeb.Data;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionWeb.ViewComponents;

public class GreetingViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserDeletionService _userDeletionService;

    public GreetingViewComponent(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        IUserDeletionService userDeletionService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _userDeletionService = userDeletionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Return empty if not authenticated
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        // Get user's display name from Client table first, then Member table
        var userEmail = User.Identity.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            return Content(string.Empty);
        }

        string displayName = userEmail;

        // Try Client table first
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == userEmail);
        
        if (client != null && !string.IsNullOrWhiteSpace(client.FirstName))
        {
            displayName = $"{client.FirstName} {client.LastName}".Trim();
        }
        else
        {
            // Try Member table via API
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user != null)
            {
                var member = await _userDeletionService.GetMemberByUserIdAsync(user.Id);
                if (member != null && !string.IsNullOrWhiteSpace(member.Name))
                {
                    displayName = member.Name;
                }
            }
        }

        return View("Default", displayName);
    }
}
