using KfConstructionWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionWeb.ViewComponents;

public class GreetingViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _dbContext;

    public GreetingViewComponent(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Content(string.Empty);
        }

        // Get user's display name from Client table
        var userEmail = User.Identity?.Name;
        var client = userEmail != null 
            ? await _dbContext.Clients.FirstOrDefaultAsync(c => c.Email == userEmail) 
            : null;
        
        // Use client's first and last name if available, otherwise use email
        string displayName;
        if (client != null)
        {
            displayName = $"{client.FirstName} {client.LastName}".Trim();
            // Fallback to email if names are empty
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = userEmail ?? "User";
            }
        }
        else
        {
            displayName = userEmail ?? "User";
        }

        return View("Default", displayName);
    }
}
