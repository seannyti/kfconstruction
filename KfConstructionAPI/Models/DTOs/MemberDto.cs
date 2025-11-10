namespace KfConstructionAPI.Models.DTOs;

/// <summary>
/// Data transfer object representing a member in API responses
/// </summary>
public class MemberDto
{
    /// <summary>
    /// The unique identifier of the member
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
    /// The date and time when the member was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Indicates whether the member is currently active in the system
    /// </summary>
    public bool IsActive { get; set; }
}
