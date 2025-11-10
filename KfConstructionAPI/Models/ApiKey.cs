using System.ComponentModel.DataAnnotations;

namespace KfConstructionAPI.Models;

/// <summary>
/// Represents an API key used to access protected endpoints.
/// Stores only a SHA256 hash of the key for security.
/// </summary>
public class ApiKey
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string KeyHash { get; set; } = string.Empty; // SHA256 hex

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // Friendly name

    [MaxLength(300)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public int UsageCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? RevokedBy { get; set; }
}
