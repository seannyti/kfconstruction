using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionWeb.Services;

/// <summary>
/// Service for managing broadcast messages to users
/// </summary>
public class BroadcastMessageService : IBroadcastMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<BroadcastMessageService> _logger;

    public BroadcastMessageService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IEmailService emailService,
        IActivityLogService activityLogService,
        ILogger<BroadcastMessageService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BroadcastMessage> SendMessageAsync(
        string subject,
        string message,
        string messageType,
        string? targetRole,
        bool sendEmail,
        bool showInApp,
        string sentBy,
        DateTime? expiresAt = null)
    {
        // Create broadcast message
        var broadcastMessage = new BroadcastMessage
        {
            Subject = subject,
            Message = message,
            MessageType = messageType,
            TargetRole = targetRole,
            SendEmail = sendEmail,
            ShowInApp = showInApp,
            SentBy = sentBy,
            SentAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        _context.BroadcastMessages.Add(broadcastMessage);
        await _context.SaveChangesAsync();

        // Get target users
        IList<IdentityUser> targetUsers;
        if (string.IsNullOrEmpty(targetRole))
        {
            targetUsers = await _userManager.Users.ToListAsync();
        }
        else
        {
            targetUsers = await _userManager.GetUsersInRoleAsync(targetRole);
        }

        broadcastMessage.RecipientCount = targetUsers.Count;

        // Create user message statuses for in-app display
        if (showInApp)
        {
            foreach (var user in targetUsers)
            {
                var userStatus = new UserMessageStatus
                {
                    BroadcastMessageId = broadcastMessage.Id,
                    UserId = user.Id,
                    IsRead = false,
                    IsDismissed = false
                };
                _context.UserMessageStatuses.Add(userStatus);
            }
        }

        await _context.SaveChangesAsync();

        // Send emails if requested
        if (sendEmail)
        {
            foreach (var user in targetUsers)
            {
                try
                {
                    // Use contact form as generic email method
                    await _emailService.SendContactFormAsync(
                        user.UserName ?? "User",
                        user.Email!,
                        subject,
                        message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send broadcast email to {Email}", user.Email);
                }
            }
        }

        // Log activity
        await _activityLogService.LogActivityAsync(
            category: ActivityCategories.System,
            action: "Broadcast Message Sent",
            performedBy: sentBy,
            description: $"Sent broadcast message '{subject}' to {targetUsers.Count} users (Role: {targetRole ?? "All"})",
            entityType: "BroadcastMessage",
            entityId: broadcastMessage.Id,
            severity: ActivitySeverity.Info
        );

        return broadcastMessage;
    }

    public async Task<IEnumerable<BroadcastMessage>> GetAllMessagesAsync(bool activeOnly = false)
    {
        var query = _context.BroadcastMessages
            .Include(bm => bm.UserStatuses)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(bm => bm.IsActive && (bm.ExpiresAt == null || bm.ExpiresAt > DateTime.UtcNow));
        }

        return await query
            .OrderByDescending(bm => bm.SentAt)
            .ToListAsync();
    }

    public async Task<BroadcastMessage?> GetMessageByIdAsync(int id)
    {
        return await _context.BroadcastMessages
            .Include(bm => bm.UserStatuses)
            .FirstOrDefaultAsync(bm => bm.Id == id);
    }

    public async Task<IEnumerable<BroadcastMessage>> GetUnreadMessagesForUserAsync(string userId)
    {
        var now = DateTime.UtcNow;

        return await _context.BroadcastMessages
            .Where(bm => bm.IsActive && bm.ShowInApp)
            .Where(bm => bm.ExpiresAt == null || bm.ExpiresAt > now)
            .Where(bm => bm.UserStatuses.Any(ums => 
                ums.UserId == userId && 
                !ums.IsRead && 
                !ums.IsDismissed))
            .OrderByDescending(bm => bm.SentAt)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int messageId, string userId)
    {
        var status = await _context.UserMessageStatuses
            .FirstOrDefaultAsync(ums => 
                ums.BroadcastMessageId == messageId && 
                ums.UserId == userId);

        if (status != null && !status.IsRead)
        {
            status.IsRead = true;
            status.ReadAt = DateTime.UtcNow;

            // Update read count on message
            var message = await _context.BroadcastMessages.FindAsync(messageId);
            if (message != null)
            {
                message.ReadCount++;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task DismissMessageAsync(int messageId, string userId)
    {
        var status = await _context.UserMessageStatuses
            .FirstOrDefaultAsync(ums => 
                ums.BroadcastMessageId == messageId && 
                ums.UserId == userId);

        if (status != null)
        {
            status.IsDismissed = true;
            status.DismissedAt = DateTime.UtcNow;

            if (!status.IsRead)
            {
                status.IsRead = true;
                status.ReadAt = DateTime.UtcNow;

                // Update read count
                var message = await _context.BroadcastMessages.FindAsync(messageId);
                if (message != null)
                {
                    message.ReadCount++;
                }
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> DeactivateMessageAsync(int messageId)
    {
        var message = await _context.BroadcastMessages.FindAsync(messageId);
        if (message == null) return false;

        message.IsActive = false;
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            category: ActivityCategories.System,
            action: "Broadcast Message Deactivated",
            performedBy: "System",
            description: $"Deactivated broadcast message '{message.Subject}'",
            entityType: "BroadcastMessage",
            entityId: messageId,
            severity: ActivitySeverity.Info
        );

        return true;
    }

    public async Task<bool> DeleteMessageAsync(int messageId)
    {
        var message = await _context.BroadcastMessages
            .Include(bm => bm.UserStatuses)
            .FirstOrDefaultAsync(bm => bm.Id == messageId);

        if (message == null) return false;

        _context.BroadcastMessages.Remove(message);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            category: ActivityCategories.System,
            action: "Broadcast Message Deleted",
            performedBy: "System",
            description: $"Deleted broadcast message '{message.Subject}'",
            entityType: "BroadcastMessage",
            entityId: messageId,
            severity: ActivitySeverity.Warning
        );

        return true;
    }

    public async Task<(int TotalSent, int Active, int TotalRecipients, int TotalRead)> GetStatisticsAsync()
    {
        var totalSent = await _context.BroadcastMessages.CountAsync();
        var active = await _context.BroadcastMessages
            .CountAsync(bm => bm.IsActive && (bm.ExpiresAt == null || bm.ExpiresAt > DateTime.UtcNow));
        var totalRecipients = await _context.BroadcastMessages.SumAsync(bm => bm.RecipientCount);
        var totalRead = await _context.BroadcastMessages.SumAsync(bm => bm.ReadCount);

        return (totalSent, active, totalRecipients, totalRead);
    }
}
