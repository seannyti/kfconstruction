using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models.ViewModels;

public abstract class BaseUserActionViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public string UserEmail { get; set; } = string.Empty;
}

public class LockAccountViewModel : BaseUserActionViewModel
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Reason for Lock")]
    public string Reason { get; set; } = string.Empty;
    
    [Display(Name = "Lock Duration")]
    public LockDuration Duration { get; set; } = LockDuration.OneDay;
    
    [Display(Name = "Custom Duration (hours)")]
    [Range(1, 8760, ErrorMessage = "Custom duration must be between 1 and 8760 hours (1 year)")]
    public int? CustomHours { get; set; }
}

public class UnlockAccountViewModel : BaseUserActionViewModel
{
    [MaxLength(200)]
    [Display(Name = "Reason for Unlock")]
    public string? UnlockReason { get; set; }
}

public class AccountLockHistoryViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public List<AccountLockEntryViewModel> LockHistory { get; set; } = new();
}

public class AccountLockEntryViewModel
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool IsPermanent { get; set; }
    public string LockedBy { get; set; } = string.Empty;
    public DateTime? UnlockedAt { get; set; }
    public string? UnlockedBy { get; set; }
    public string? UnlockReason { get; set; }
    public bool IsActive { get; set; }
    public string LockStatusText { get; set; } = string.Empty;
}

public enum LockDuration
{
    [Display(Name = "1 Hour")]
    OneHour = 1,
    
    [Display(Name = "6 Hours")]
    SixHours = 6,
    
    [Display(Name = "1 Day")]
    OneDay = 24,
    
    [Display(Name = "3 Days")]
    ThreeDays = 72,
    
    [Display(Name = "1 Week")]
    OneWeek = 168,
    
    [Display(Name = "1 Month")]
    OneMonth = 744,
    
    [Display(Name = "Permanent")]
    Permanent = -1,
    
    [Display(Name = "Custom")]
    Custom = 0
}