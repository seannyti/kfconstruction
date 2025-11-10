using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace KfConstructionWeb.Models;

public class AccountLock
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;
    
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LockedUntil { get; set; }
    
    public bool IsPermanent { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string LockedBy { get; set; } = string.Empty;
    
    public DateTime? UnlockedAt { get; set; }
    
    [MaxLength(100)]
    public string? UnlockedBy { get; set; }
    
    [MaxLength(200)]
    public string? UnlockReason { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation property
    public virtual IdentityUser? User { get; set; }
    
    // Helper properties
    public bool IsCurrentlyLocked => IsActive && (IsPermanent || (LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow));
    
    public string LockStatusText => IsActive ? 
        (IsPermanent ? "Permanently Locked" : 
         LockedUntil.HasValue ? $"Locked until {LockedUntil.Value:MMM dd, yyyy HH:mm}" : "Locked") : 
        "Unlocked";
}