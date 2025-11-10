using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace KfConstructionWeb.Models;

/// <summary>
/// Represents a client who can access project timelines and information
/// </summary>
public class Client
{
    /// <summary>
    /// Unique identifier for the client
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Client's first name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Client's last name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Client's email address (used for login)
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Client's phone number
    /// </summary>
    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Client's address
    /// </summary>
    [StringLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Company name if applicable
    /// </summary>
    [StringLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Associated ASP.NET Identity user ID
    /// </summary>
    [StringLength(450)]
    public string? UserId { get; set; }

    /// <summary>
    /// Whether this client account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Unique access code for client portal
    /// </summary>
    [StringLength(50)]
    public string? AccessCode { get; set; }

    /// <summary>
    /// When this client was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this client record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the associated Identity user
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual IdentityUser? User { get; set; }

    /// <summary>
    /// Full name for display purposes
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Display name including company if available
    /// </summary>
    [NotMapped]
    public string DisplayName => string.IsNullOrEmpty(CompanyName) ? FullName : $"{FullName} ({CompanyName})";
}