using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models;

/// <summary>
/// Unified activity log for audit trail and system events
/// </summary>
public class ActivityLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // User, Email, Receipt, File, Testimonial, System, Security

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, Login, Logout, Upload, Download, etc.

    [Required]
    [MaxLength(256)]
    public string PerformedBy { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? EntityType { get; set; } // User, Receipt, File, etc.

    public int? EntityId { get; set; }

    [MaxLength(256)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical

    public string? AdditionalData { get; set; } // JSON for extra context
}

/// <summary>
/// Activity categories
/// </summary>
public static class ActivityCategories
{
    public const string User = "User";
    public const string Email = "Email";
    public const string Receipt = "Receipt";
    public const string File = "File";
    public const string Testimonial = "Testimonial";
    public const string System = "System";
    public const string Security = "Security";
    public const string Settings = "Settings";

    public static List<string> GetAll() => new()
    {
        User, Email, Receipt, File, Testimonial, System, Security, Settings
    };
}

/// <summary>
/// Activity severity levels
/// </summary>
public static class ActivitySeverity
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Critical = "Critical";

    public static List<string> GetAll() => new()
    {
        Info, Warning, Error, Critical
    };
}
