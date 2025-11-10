using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models;

public class AppSetting
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string Category { get; set; } = "General";
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? ModifiedBy { get; set; }
}