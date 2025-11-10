using KfConstructionWeb.Models;
using KfConstructionWeb.Models.DTOs;

namespace KfConstructionWeb.Models.ViewModels;

/// <summary>
/// View model for displaying user information in the admin area
/// </summary>
public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// View model for editing user information
/// </summary>
public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
}

/// <summary>
/// View model for displaying detailed user profile information
/// </summary>
public class UserProfileViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
    public DateTime AccountCreated { get; set; }
    public MemberDto? MemberData { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    
    // Account Lock Information
    public bool IsCurrentlyLocked { get; set; }
    public AccountLock? CurrentLock { get; set; }
    public bool CanManageLocks { get; set; }
}
