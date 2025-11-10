namespace KfConstructionWeb.Models.DTOs;

/// <summary>
/// DTO for member data received from API
/// </summary>
public class MemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating a new member via API
/// </summary>
public class CreateMemberDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public bool IsActive { get; set; } = true;
}
