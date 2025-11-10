using KfConstructionWeb.Models;
using KfConstructionWeb.Models.ViewModels;

namespace KfConstructionWeb.Services.Interfaces;

public interface IAccountLockService
{
    Task<bool> LockAccountAsync(string userId, string reason, LockDuration duration, int? customHours, string lockedBy);
    Task<bool> UnlockAccountAsync(string userId, string? unlockReason, string unlockedBy);
    Task<bool> IsAccountLockedAsync(string userId);
    Task<AccountLock?> GetActiveLockAsync(string userId);
    Task<List<AccountLock>> GetLockHistoryAsync(string userId);
    Task<List<AccountLock>> GetAllActiveLocksAsync();
    Task<bool> CleanupExpiredLocksAsync();
}