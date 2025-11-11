namespace KfConstructionAPI.Models;

/// <summary>
/// Represents a member entity in the construction management system
/// </summary>
public class Member
{
    /// <summary>
    /// The unique identifier for the member
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The full name of the member
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the member
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The associated ASP.NET Identity user ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The date and time when the member was created (defaults to UTC now)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates whether the member is currently active in the system (defaults to true)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
