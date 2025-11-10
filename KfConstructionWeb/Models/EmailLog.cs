using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models;

/// <summary>
/// Entity for tracking email communications
/// </summary>
public class EmailLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string ToEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FromEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    public string? BodyText { get; set; }

    public string? BodyHtml { get; set; }

    public DateTime? SentAt { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed, Disabled

    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string EmailType { get; set; } = string.Empty; // Testimonial, Contact, ProjectUpdate, etc.

    /// <summary>
    /// Reference to related entity (testimonial, project, etc.)
    /// </summary>
    public int? RelatedEntityId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of retry attempts for failed emails
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// When to retry sending (for failed emails)
    /// </summary>
    public DateTime? RetryAt { get; set; }
}