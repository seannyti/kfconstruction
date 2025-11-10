using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models;

/// <summary>
/// Represents a broadcast message sent to users
/// </summary>
public class BroadcastMessage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string MessageType { get; set; } = "Info"; // Info, Warning, Alert, Announcement

    [MaxLength(50)]
    public string? TargetRole { get; set; } // null = all users, or specific role

    public bool SendEmail { get; set; } = false;

    public bool ShowInApp { get; set; } = true;

    [MaxLength(256)]
    public string SentBy { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public int RecipientCount { get; set; } = 0;

    public int ReadCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<UserMessageStatus> UserStatuses { get; set; } = new List<UserMessageStatus>();
}

/// <summary>
/// Tracks which users have read a broadcast message
/// </summary>
public class UserMessageStatus
{
    public int Id { get; set; }

    public int BroadcastMessageId { get; set; }

    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public bool IsDismissed { get; set; } = false;

    public DateTime? DismissedAt { get; set; }

    // Navigation property
    public BroadcastMessage BroadcastMessage { get; set; } = null!;
}

/// <summary>
/// Message types for broadcast messages
/// </summary>
public static class MessageTypes
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Alert = "Alert";
    public const string Announcement = "Announcement";
    public const string Maintenance = "Maintenance";

    public static List<string> GetAll() => new()
    {
        Info, Warning, Alert, Announcement, Maintenance
    };
}
