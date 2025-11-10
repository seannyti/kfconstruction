namespace KfConstructionAPI.Models.DTOs;

/// <summary>
/// DTO representing client data from the web application
/// </summary>
public class WebClientDto
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
    public int ProjectCount { get; set; }
}

/// <summary>
/// DTO representing aggregated user data across all systems
/// </summary>
public class CrossSystemUserProfileDto
{
    public string Email { get; set; } = string.Empty;
    public WebClientDto? WebClient { get; set; }
    public MemberDto? ApiMember { get; set; }
    
    /// <summary>
    /// Indicates if the user exists in the web application
    /// </summary>
    public bool HasWebAccount => WebClient != null;
    
    /// <summary>
    /// Indicates if the user exists in the API system
    /// </summary>
    public bool HasApiMembership => ApiMember != null;
    
    /// <summary>
    /// Primary display name from either system
    /// </summary>
    public string DisplayName => WebClient?.FullName ?? ApiMember?.Name ?? Email;
    
    /// <summary>
    /// Combined active status from both systems
    /// </summary>
    public bool IsActive => (WebClient?.IsActive ?? false) || (ApiMember?.IsActive ?? false);
}