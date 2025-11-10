namespace KfConstructionWeb.Models.DTOs;

/// <summary>
/// DTO for client data used in cross-system API communication
/// </summary>
public class ClientApiDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? CompanyName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}
