using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.ViewModels;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionWeb.Services;

public class AccountLockService : IAccountLockService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountLockService> _logger;

    public AccountLockService(ApplicationDbContext context, ILogger<AccountLockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> LockAccountAsync(string userId, string reason, LockDuration duration, int? customHours, string lockedBy)
    {
        try
        {
            // First, unlock any existing active locks for this user
            await UnlockPreviousLocksAsync(userId, "Superseded by new lock", lockedBy);

            var lockUntil = CalculateLockUntil(duration, customHours);
            var isPermanent = duration == LockDuration.Permanent;

            var accountLock = new AccountLock
            {
                UserId = userId,
                Reason = reason,
                LockedAt = DateTime.UtcNow,
                LockedUntil = lockUntil,
                IsPermanent = isPermanent,
                LockedBy = lockedBy,
                IsActive = true
            };

            _context.AccountLocks.Add(accountLock);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Account {UserId} locked by {LockedBy}. Reason: {Reason}. Duration: {Duration}", 
                userId, lockedBy, reason, duration);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking account {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnlockAccountAsync(string userId, string? unlockReason, string unlockedBy)
    {
        try
        {
            var activeLock = await GetActiveLockAsync(userId);
            if (activeLock == null)
            {
                return false; // No active lock to unlock
            }

            activeLock.IsActive = false;
            activeLock.UnlockedAt = DateTime.UtcNow;
            activeLock.UnlockedBy = unlockedBy;
            activeLock.UnlockReason = unlockReason;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Account {UserId} unlocked by {UnlockedBy}. Reason: {Reason}", 
                userId, unlockedBy, unlockReason ?? "No reason provided");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsAccountLockedAsync(string userId)
    {
        var activeLock = await GetActiveLockAsync(userId);
        return activeLock?.IsCurrentlyLocked ?? false;
    }

    public async Task<AccountLock?> GetActiveLockAsync(string userId)
    {
        return await _context.AccountLocks
            .Where(l => l.UserId == userId && l.IsActive)
            .OrderByDescending(l => l.LockedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AccountLock>> GetLockHistoryAsync(string userId)
    {
        return await _context.AccountLocks
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.LockedAt)
            .ToListAsync();
    }

    public async Task<List<AccountLock>> GetAllActiveLocksAsync()
    {
        return await _context.AccountLocks
            .Include(l => l.User)
            .Where(l => l.IsActive)
            .OrderByDescending(l => l.LockedAt)
            .ToListAsync();
    }

    public async Task<bool> CleanupExpiredLocksAsync()
    {
        try
        {
            var expiredLocks = await _context.AccountLocks
                .Where(l => l.IsActive && 
                           !l.IsPermanent && 
                           l.LockedUntil.HasValue && 
                           l.LockedUntil.Value <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var expiredLock in expiredLocks)
            {
                expiredLock.IsActive = false;
                expiredLock.UnlockedAt = DateTime.UtcNow;
                expiredLock.UnlockedBy = "System";
                expiredLock.UnlockReason = "Lock expired automatically";
            }

            if (expiredLocks.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired account locks", expiredLocks.Count);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired locks");
            return false;
        }
    }

    private async Task UnlockPreviousLocksAsync(string userId, string reason, string unlockedBy)
    {
        var previousLocks = await _context.AccountLocks
            .Where(l => l.UserId == userId && l.IsActive)
            .ToListAsync();

        foreach (var previousLock in previousLocks)
        {
            previousLock.IsActive = false;
            previousLock.UnlockedAt = DateTime.UtcNow;
            previousLock.UnlockedBy = unlockedBy;
            previousLock.UnlockReason = reason;
        }
    }

    private static DateTime? CalculateLockUntil(LockDuration duration, int? customHours)
    {
        return duration switch
        {
            LockDuration.Permanent => null,
            LockDuration.Custom when customHours.HasValue => DateTime.UtcNow.AddHours(customHours.Value),
            _ => DateTime.UtcNow.AddHours((int)duration)
        };
    }
}