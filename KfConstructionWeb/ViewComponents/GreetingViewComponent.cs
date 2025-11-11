using KfConstructionWeb.Data;
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
        // Return empty if not authenticated
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        // Get user's display name from Client table
        var userEmail = User.Identity.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            return Content(string.Empty);
        }

        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == userEmail);
        
        // Use client's first and last name if available, otherwise use email
        string displayName;
        if (client != null && !string.IsNullOrWhiteSpace(client.FirstName))
        {
            displayName = $"{client.FirstName} {client.LastName}".Trim();
        }
        else
        {
            displayName = userEmail;
        }

        return View("Default", displayName);
    }
}
