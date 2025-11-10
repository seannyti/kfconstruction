using System.ComponentModel.DataAnnotations;

namespace KfConstructionAPI.Models.DTOs;

/// <summary>
/// DTO for creating members with cross-system awareness
/// </summary>
public class SmartCreateMemberDto : BaseMemberDto
{
    /// <summary>
    /// Whether to attempt linking with existing web account
    /// </summary>
    public bool LinkWithWebAccount { get; set; } = true;
    
    /// <summary>
    /// Additional information for potential web account creation
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// Additional information for potential web account creation
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// Phone number for potential web account creation
    /// </summary>
    [Phone]
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Company name for potential web account creation
    /// </summary>
    public string? CompanyName { get; set; }
}