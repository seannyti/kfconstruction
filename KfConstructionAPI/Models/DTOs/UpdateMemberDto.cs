using System.ComponentModel.DataAnnotations;

namespace KfConstructionAPI.Models.DTOs;

/// <summary>
/// Data transfer object for updating existing members
/// </summary>
public class UpdateMemberDto : BaseMemberDto
{
    /// <summary>
    /// The unique identifier of the member to update
    /// </summary>
    [Required(ErrorMessage = "Member ID is required")]
    public int Id { get; set; }
}
