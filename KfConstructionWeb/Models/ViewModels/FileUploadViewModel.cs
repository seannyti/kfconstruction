using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models.ViewModels;

public class FileUploadViewModel
{
    [Required(ErrorMessage = "Please select a file to upload")]
    public IFormFile? File { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; } = FileCategories.General;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public bool IsPublic { get; set; } = false;

    public bool EncryptFile { get; set; } = false;
}

public class FileSearchViewModel
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public DateTime? UploadedAfter { get; set; }
    public DateTime? UploadedBefore { get; set; }
    public string? UploadedBy { get; set; }
    public bool? IsPublic { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
