using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models;

/// <summary>
/// Represents an uploaded file in the system
/// </summary>
public class UploadedFile
{
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [MaxLength(50)]
    public string Category { get; set; } = "General";

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public bool IsPublic { get; set; } = false;

    public bool IsEncrypted { get; set; } = false;

    [MaxLength(50)]
    public string? EncryptionAlgorithm { get; set; }

    [Required]
    [MaxLength(256)]
    public string UploadedBy { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int DownloadCount { get; set; } = 0;

    public DateTime? LastAccessedAt { get; set; }

    [MaxLength(256)]
    public string? LastAccessedBy { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    [MaxLength(256)]
    public string? DeletedBy { get; set; }

    public DateTime? ScheduledPurgeDate { get; set; }

    // Computed property
    public string FileSizeFormatted => FormatFileSize(FileSizeBytes);

    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// File categories for organization
/// </summary>
public static class FileCategories
{
    public const string General = "General";
    public const string ProjectPhotos = "Project Photos";
    public const string Documents = "Documents";
    public const string Contracts = "Contracts";
    public const string Invoices = "Invoices";
    public const string Marketing = "Marketing";
    public const string Legal = "Legal";
    public const string Safety = "Safety";
    public const string Other = "Other";

    public static List<string> GetAll() => new()
    {
        General,
        ProjectPhotos,
        Documents,
        Contracts,
        Invoices,
        Marketing,
        Legal,
        Safety,
        Other
    };
}
