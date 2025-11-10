using Microsoft.AspNetCore.Identity;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Middleware;

public class AccountLockMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AccountLockMiddleware> _logger;

    public AccountLockMiddleware(RequestDelegate next, ILogger<AccountLockMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, 
        UserManager<IdentityUser> userManager, 
        SignInManager<IdentityUser> signInManager,
        IAccountLockService accountLockService)
    {
        // Skip for anonymous users
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Skip for certain paths (logout, access denied, etc.)
        var skipPaths = MiddlewareHelpers.CommonSkipPaths
            .Concat(MiddlewareHelpers.AuthPaths)
            .Concat(new[] { "/home/accessdenied", "/home/error" })
            .ToArray();

        if (MiddlewareHelpers.ShouldSkipPath(context.Request.Path.Value, skipPaths))
        {
            await _next(context);
            return;
        }

        try
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                // Check if account is locked
                var isLocked = await accountLockService.IsAccountLockedAsync(user.Id);
                if (isLocked)
                {
                    _logger.LogWarning("Locked user {UserId} attempted to access {Path}", user.Id, context.Request.Path.Value);
                    
                    // Sign out the user
                    await signInManager.SignOutAsync();
                    
                    // Redirect to access denied with lock message
                    context.Response.Redirect("/Home/AccessDenied?reason=account-locked");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking account lock status");
            // Continue processing - don't block legitimate users due to errors
        }

        await _next(context);
    }
}