using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.ViewModels;
using KfConstructionWeb.Services.Interfaces;
using KfConstructionWeb.Data;
using System.Text.Json;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserDeletionService _userDeletionService;
    private readonly IAccountLockService _accountLockService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IHttpClientFactory httpClientFactory,
        IUserDeletionService userDeletionService,
        IAccountLockService accountLockService,
        ApplicationDbContext dbContext,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _httpClientFactory = httpClientFactory;
        _userDeletionService = userDeletionService;
        _accountLockService = accountLockService;
        _dbContext = dbContext;
        _logger = logger;
    }

    #region Helper Methods

    private async Task<IActionResult?> ValidateUserExists(string? id, string? redirectAction = null)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return redirectAction != null ? RedirectToAction(redirectAction) : RedirectToAction(nameof(Index));
        }

        return null; // User exists, continue processing
    }
    
    private async Task<IdentityUser?> GetValidatedUser(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return await _userManager.FindByIdAsync(id);
    }

    private async Task<bool> IsCurrentUser(string userId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        return currentUser?.Id == userId;
    }

    private IActionResult ErrorRedirect(string message, string action = "Index", object? routeValues = null)
    {
        TempData["Error"] = message;
        return RedirectToAction(action, routeValues);
    }

    private IActionResult SuccessRedirect(string message, string action = "Index", object? routeValues = null)
    {
        TempData["Success"] = message;
        return RedirectToAction(action, routeValues);
    }

    private string GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    #endregion

    // GET: Admin/Users
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UserViewModel>();
        var currentUserIsSuperAdmin = User.IsInRole("SuperAdmin");

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isSuperAdmin = roles.Contains("SuperAdmin");
            
            // Hide SuperAdmin users from regular Admins
            if (isSuperAdmin && !currentUserIsSuperAdmin)
            {
                continue; // Skip this user
            }

            var member = await _userDeletionService.GetMemberByUserIdAsync(user.Id);

            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Name = member?.Name ?? "N/A",
                IsAdmin = roles.Contains("Admin"),
                IsSuperAdmin = isSuperAdmin,
                CreatedAt = member?.CreatedAt ?? DateTime.MinValue
            });
        }

        return View(userViewModels);
    }

    // GET: Admin/Users/Edit/{id}
    public async Task<IActionResult> Edit(string id)
    {
        var user = await GetValidatedUser(id);
        if (user == null)
            return ErrorRedirect("User not found.");

        var member = await _userDeletionService.GetMemberByUserIdAsync(id);
        var roles = await _userManager.GetRolesAsync(user);

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Name = member?.Name ?? "",
            IsAdmin = roles.Contains("Admin"),
            IsSuperAdmin = roles.Contains("SuperAdmin")
        };

        return View(model);
    }

    // POST: Admin/Users/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await GetValidatedUser(model.Id);
        if (user == null)
            return ErrorRedirect("User not found.");

        // Update email if changed
        if (user.Email != model.Email)
        {
            user.Email = model.Email;
            user.UserName = model.Email;
            await _userManager.UpdateAsync(user);
        }

        // Update member info via API
        var member = await _userDeletionService.GetMemberByUserIdAsync(model.Id);
        if (member != null)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("KfConstructionAPI");
                var updateData = new
                {
                    id = member.Id,
                    name = model.Name,
                    email = model.Email,
                    userId = model.Id,
                    createdAt = member.CreatedAt,
                    isActive = member.IsActive
                };

                var response = await httpClient.PutAsJsonAsync($"/api/v1/Members/{member.Id}", updateData);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "User information updated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to update member information.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating member for user {model.Id}");
                TempData["Error"] = "An error occurred while updating member information.";
            }
        }

        return RedirectToAction(nameof(ViewProfile), new { id = model.Id });
    }

    // POST: Admin/Users/ToggleAdmin
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleAdmin(string userId)
    {
        var user = await GetValidatedUser(userId);
        if (user == null)
            return ErrorRedirect("User not found.");

        // Prevent self-demotion
        if (await IsCurrentUser(userId))
            return ErrorRedirect("You cannot change your own admin role.");

        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains("Admin");

        if (isAdmin)
        {
            // Remove Admin role
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            
            // Ensure User role is assigned
            if (!roles.Contains("User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
            
            TempData["Success"] = $"User {user.Email} is now a regular member.";
        }
        else
        {
            // Add Admin role
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["Success"] = $"User {user.Email} is now an administrator.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Users/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(string userId)
    {
        var user = await GetValidatedUser(userId);
        if (user == null)
            return ErrorRedirect("User not found.");

        // Don't allow deleting yourself
        if (await IsCurrentUser(userId))
            return ErrorRedirect("You cannot delete your own account.");

        // Use centralized deletion service
        var success = await _userDeletionService.DeleteUserCompletelyAsync(userId);
        
        return success 
            ? SuccessRedirect($"User {user.Email} has been deleted completely.")
            : ErrorRedirect("Failed to delete user completely.");
    }

    [HttpGet]
    public async Task<IActionResult> ViewProfile(string id)
    {
        var user = await GetValidatedUser(id);
        if (user == null)
            return NotFound();

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains("Admin");
        var isSuperAdmin = roles.Contains("SuperAdmin");

        // Get member data from API
        var memberData = await _userDeletionService.GetMemberByUserIdAsync(id);

        // Get client data for full name
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == user.Email);
        var fullName = client != null ? $"{client.FirstName} {client.LastName}".Trim() : null;

        // Get current lock status
        var currentLock = await _accountLockService.GetActiveLockAsync(id);
        var isCurrentlyLocked = currentLock != null && currentLock.IsCurrentlyLocked;

        // Check if current user can manage locks (SuperAdmin only)
        var currentUser = await _userManager.GetUserAsync(User);
        var currentUserRoles = currentUser != null ? await _userManager.GetRolesAsync(currentUser) : new List<string>();
        var canManageLocks = currentUserRoles.Contains("SuperAdmin");

        var viewModel = new UserProfileViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            FullName = fullName,
            IsAdmin = isAdmin,
            IsSuperAdmin = isSuperAdmin,
            MemberData = memberData,
            AccountCreated = DateTime.Now, // You might want to add a proper CreatedAt field to IdentityUser
            IsCurrentlyLocked = isCurrentlyLocked,
            CurrentLock = currentLock,
            CanManageLocks = canManageLocks,
            ClientIp = GetClientIpAddress()
        };

        return View(viewModel);
    }

    // GET: Admin/Users/Lock/{id}
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Lock(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await GetValidatedUser(id);
        if (user == null)
            return NotFound();

        // Prevent locking yourself
        if (await IsCurrentUser(id))
            return ErrorRedirect("You cannot lock your own account.", nameof(ViewProfile), new { id });

        // Check if already locked
        var isLocked = await _accountLockService.IsAccountLockedAsync(id);
        if (isLocked)
            return ErrorRedirect("This account is already locked.", nameof(ViewProfile), new { id });

        var model = new LockAccountViewModel
        {
            UserId = id,
            UserEmail = user.Email ?? ""
        };

        return View(model);
    }

    // POST: Admin/Users/Lock
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Lock(LockAccountViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await GetValidatedUser(model.UserId);
        if (user == null)
            return ErrorRedirect("User not found.");

        var currentUser = await _userManager.GetUserAsync(User);
        var lockedBy = currentUser?.Email ?? "System";

        var success = await _accountLockService.LockAccountAsync(
            model.UserId, 
            model.Reason, 
            model.Duration, 
            model.CustomHours, 
            lockedBy);

        return success 
            ? SuccessRedirect($"Account {user.Email} has been locked successfully.", nameof(ViewProfile), new { id = model.UserId })
            : ErrorRedirect("Failed to lock the account. Please try again.", nameof(ViewProfile), new { id = model.UserId });
    }

    // POST: Admin/Users/Unlock/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Unlock(string id, string? unlockReason)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var unlockedBy = currentUser?.Email ?? "System";

        var success = await _accountLockService.UnlockAccountAsync(id, unlockReason, unlockedBy);

        if (success)
        {
            TempData["Success"] = $"Account {user.Email} has been unlocked successfully.";
        }
        else
        {
            TempData["Error"] = "Failed to unlock the account or account was not locked.";
        }

        return RedirectToAction(nameof(ViewProfile), new { id });
    }

    // GET: Admin/Users/LockHistory/{id}
    [HttpGet]
    public async Task<IActionResult> LockHistory(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var lockHistory = await _accountLockService.GetLockHistoryAsync(id);
        
        var model = new AccountLockHistoryViewModel
        {
            UserId = id,
            UserEmail = user.Email ?? "",
            LockHistory = lockHistory.Select(l => new AccountLockEntryViewModel
            {
                Id = l.Id,
                Reason = l.Reason,
                LockedAt = l.LockedAt,
                LockedUntil = l.LockedUntil,
                IsPermanent = l.IsPermanent,
                LockedBy = l.LockedBy,
                UnlockedAt = l.UnlockedAt,
                UnlockedBy = l.UnlockedBy,
                UnlockReason = l.UnlockReason,
                IsActive = l.IsActive,
                LockStatusText = l.LockStatusText
            }).ToList()
        };

        return View(model);
    }

    // POST: Admin/Users/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ResetPassword(string userId, string newPassword, bool forcePasswordChange = false)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newPassword))
        {
            TempData["Error"] = "User ID and new password are required.";
            return RedirectToAction(nameof(ViewProfile), new { id = userId });
        }

        var user = await GetValidatedUser(userId);
        if (user == null)
            return ErrorRedirect("User not found.");

        // Prevent resetting SuperAdmin passwords (additional safety)
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("SuperAdmin"))
        {
            TempData["Error"] = "Cannot reset password for SuperAdmin accounts.";
            return RedirectToAction(nameof(ViewProfile), new { id = userId });
        }

        try
        {
            // Generate password reset token and reset password
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                // If force password change is enabled, update security stamp to invalidate existing sessions
                if (forcePasswordChange)
                {
                    await _userManager.UpdateSecurityStampAsync(user);
                    _logger.LogInformation($"Password reset for user {user.Email} with forced password change on next login by {User.Identity?.Name}");
                    TempData["Success"] = $"Password reset successfully. User will be required to change password on next login.";
                }
                else
                {
                    _logger.LogInformation($"Password reset for user {user.Email} by {User.Identity?.Name}");
                    TempData["Success"] = "Password reset successfully.";
                }
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning($"Failed to reset password for user {user.Email}: {errors}");
                TempData["Error"] = $"Failed to reset password: {errors}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error resetting password for user {userId}");
            TempData["Error"] = "An error occurred while resetting the password. Please try again.";
        }

        return RedirectToAction(nameof(ViewProfile), new { id = userId });
    }
}
