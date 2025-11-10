using KfConstructionWeb.Models;

namespace KfConstructionWeb.Services.Interfaces;

/// <summary>
/// Service for managing broadcast messages to users
/// </summary>
public interface IBroadcastMessageService
{
    /// <summary>
    /// Send a broadcast message
    /// </summary>
    Task<BroadcastMessage> SendMessageAsync(
        string subject,
        string message,
        string messageType,
        string? targetRole,
        bool sendEmail,
        bool showInApp,
        string sentBy,
        DateTime? expiresAt = null);

    /// <summary>
    /// Get all broadcast messages with optional filters
    /// </summary>
    Task<IEnumerable<BroadcastMessage>> GetAllMessagesAsync(bool activeOnly = false);

    /// <summary>
    /// Get message by ID
    /// </summary>
    Task<BroadcastMessage?> GetMessageByIdAsync(int id);

    /// <summary>
    /// Get unread messages for a user
    /// </summary>
    Task<IEnumerable<BroadcastMessage>> GetUnreadMessagesForUserAsync(string userId);

    /// <summary>
    /// Mark message as read for a user
    /// </summary>
    Task MarkAsReadAsync(int messageId, string userId);

    /// <summary>
    /// Dismiss message for a user
    /// </summary>
    Task DismissMessageAsync(int messageId, string userId);

    /// <summary>
    /// Deactivate a broadcast message
    /// </summary>
    Task<bool> DeactivateMessageAsync(int messageId);

    /// <summary>
    /// Delete a broadcast message
    /// </summary>
    Task<bool> DeleteMessageAsync(int messageId);

    /// <summary>
    /// Get message statistics
    /// </summary>
    Task<(int TotalSent, int Active, int TotalRecipients, int TotalRead)> GetStatisticsAsync();
}
