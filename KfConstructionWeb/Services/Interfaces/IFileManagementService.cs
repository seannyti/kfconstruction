namespace KfConstructionWeb.Services.Interfaces;

/// <summary>
/// Service for managing file uploads and downloads
/// </summary>
public interface IFileManagementService
{
    /// <summary>
    /// Upload a file to the system
    /// </summary>
    Task<(bool Success, int FileId, string ErrorMessage)> UploadFileAsync(
        Stream fileStream, 
        string fileName, 
        string contentType, 
        long fileSize,
        string category,
        string? description,
        string? tags,
        bool isPublic,
        bool encryptFile,
        string uploadedBy);

    /// <summary>
    /// Download a file
    /// </summary>
    Task<(bool Success, Stream? FileStream, string? ContentType, string? FileName, string ErrorMessage)> DownloadFileAsync(int fileId, string requestedBy);

    /// <summary>
    /// Delete a file (soft delete)
    /// </summary>
    Task<bool> DeleteFileAsync(int fileId, string deletedBy);

    /// <summary>
    /// Permanently delete a file
    /// </summary>
    Task<bool> PermanentlyDeleteFileAsync(int fileId);

    /// <summary>
    /// Get file by ID
    /// </summary>
    Task<Models.UploadedFile?> GetFileByIdAsync(int fileId);

    /// <summary>
    /// Search files with filters
    /// </summary>
    Task<(List<Models.UploadedFile> Files, int TotalCount)> SearchFilesAsync(
        string? searchTerm,
        string? category,
        DateTime? uploadedAfter,
        DateTime? uploadedBefore,
        string? uploadedBy,
        bool? isPublic,
        int page,
        int pageSize);

    /// <summary>
    /// Get file categories
    /// </summary>
    List<string> GetCategories();
}
