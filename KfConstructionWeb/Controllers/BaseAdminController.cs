using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KfConstructionWeb.Controllers;

/// <summary>
/// Base controller for admin area with common security utilities
/// Reduces code duplication and provides consistent security patterns
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public abstract class BaseAdminController : Controller
{
    /// <summary>
    /// Gets the current user's unique identifier (GUID)
    /// </summary>
    /// <returns>User ID or "Unknown" if not authenticated</returns>
    protected string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
    }

    /// <summary>
    /// Gets the current user's display name
    /// </summary>
    /// <returns>Username or "Unknown" if not authenticated</returns>
    protected string GetCurrentUserName()
    {
        return User.Identity?.Name ?? "Unknown";
    }

    /// <summary>
    /// Gets the current user's email address
    /// </summary>
    /// <returns>Email or "Unknown" if not available</returns>
    protected string GetCurrentUserEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
    }

    /// <summary>
    /// Gets the client's IP address for audit logging
    /// </summary>
    /// <returns>IP address or "Unknown" if not available</returns>
    protected string GetClientIpAddress()
    {
        // Try to get real IP behind proxies/load balancers
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs (client, proxy1, proxy2...)
            // Take the first one (original client)
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fallback to direct connection IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Gets the user agent string for audit logging
    /// </summary>
    /// <returns>User agent string or "Unknown" if not available</returns>
    protected string GetUserAgent()
    {
        return HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }

    /// <summary>
    /// Checks if the current user has SuperAdmin role
    /// </summary>
    /// <returns>True if user is SuperAdmin</returns>
    protected bool IsSuperAdmin()
    {
        return User.IsInRole("SuperAdmin");
    }

    /// <summary>
    /// Checks if the current user has Admin or SuperAdmin role
    /// </summary>
    /// <returns>True if user has admin privileges</returns>
    protected bool IsAdmin()
    {
        return User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
    }

    /// <summary>
    /// Creates a standardized error message for TempData
    /// </summary>
    protected void SetErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
    }

    /// <summary>
    /// Creates a standardized success message for TempData
    /// </summary>
    protected void SetSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    /// <summary>
    /// Creates a standardized warning message for TempData
    /// </summary>
    protected void SetWarningMessage(string message)
    {
        TempData["WarningMessage"] = message;
    }

    /// <summary>
    /// Creates a standardized info message for TempData
    /// </summary>
    protected void SetInfoMessage(string message)
    {
        TempData["InfoMessage"] = message;
    }
}
