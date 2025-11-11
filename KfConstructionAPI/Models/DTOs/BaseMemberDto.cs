using System.ComponentModel.DataAnnotations;

namespace KfConstructionAPI.Models.DTOs;

/// <summary>
/// Base class for member data transfer objects containing common properties and validation
/// </summary>
public abstract class BaseMemberDto
{
    /// <summary>
    /// The full name of the member
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the member
    /// </summary>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The associated ASP.NET Identity user ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Indicates whether the member is currently active in the system
    /// </summary>
    public bool IsActive { get; set; } = true;
}