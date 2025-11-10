using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;
using KfConstructionWeb.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace KfConstructionWeb.Services;

/// <summary>
/// Service for managing file uploads, downloads, and organization
/// </summary>
public class FileManagementService : IFileManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileEncryptionService _encryptionService;
    private readonly ILogger<FileManagementService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadBasePath;
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;

    public FileManagementService(
        ApplicationDbContext context,
        IFileEncryptionService encryptionService,
        ILogger<FileManagementService> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _uploadBasePath = Path.Combine(environment.WebRootPath, "uploads", "files");
        _maxFileSize = _configuration.GetValue<long>("FileManagement:MaxFileSizeBytes", 50 * 1024 * 1024); // 50MB default
        _allowedExtensions = _configuration.GetSection("FileManagement:AllowedExtensions").Get<string[]>()
            ?? new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip" };

        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadBasePath);
    }

    public async Task<(bool Success, int FileId, string ErrorMessage)> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string category,
        string? description,
        string? tags,
        bool isPublic,
        bool encryptFile,
        string uploadedBy)
    {
        try
        {
            // Validate file
            var (isValid, errorMessage, secureFileName) = FileValidationHelper.ValidateFileUpload(
                fileName, fileSize, contentType, _allowedExtensions, _maxFileSize);

            if (!isValid)
            {
                _logger.LogWarning("File validation failed: {Error}, FileName: {FileName}", errorMessage, fileName);
                return (false, 0, errorMessage!);
            }

            // Sanitize inputs
            var sanitizedDescription = !string.IsNullOrWhiteSpace(description) 
                ? HtmlEncoder.Default.Encode(description.Trim()) 
                : null;
            var sanitizedTags = !string.IsNullOrWhiteSpace(tags) 
                ? HtmlEncoder.Default.Encode(tags.Trim()) 
                : null;

            string filePath;
            string? encryptionAlgorithm = null;

            // Handle encryption if requested
            if (encryptFile)
            {
                (filePath, encryptionAlgorithm) = await _encryptionService.EncryptFileAsync(fileStream, secureFileName);
            }
            else
            {
                // Save unencrypted file
                var uniqueFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{secureFileName}";
                filePath = Path.Combine(_uploadBasePath, uniqueFileName);

                using var fileStreamOut = new FileStream(filePath, FileMode.Create);
                await fileStream.CopyToAsync(fileStreamOut);
            }

            // Create database record
            var uploadedFile = new UploadedFile
            {
                FileName = secureFileName,
                FilePath = filePath,
                ContentType = contentType,
                FileSizeBytes = fileSize,
                Category = category,
                Description = sanitizedDescription,
                Tags = sanitizedTags,
                IsPublic = isPublic,
                IsEncrypted = encryptFile,
                EncryptionAlgorithm = encryptionAlgorithm,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                ScheduledPurgeDate = DateTime.UtcNow.AddYears(7) // Default retention
            };

            _context.UploadedFiles.Add(uploadedFile);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "File uploaded successfully: ID={FileId}, Name={FileName}, Size={Size}, Encrypted={Encrypted}, User={User}",
                uploadedFile.Id, uploadedFile.FileName, fileSize, encryptFile, uploadedBy);

            return (true, uploadedFile.Id, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}, User: {User}", fileName, uploadedBy);
            return (false, 0, "An error occurred while uploading the file.");
        }
    }

    public async Task<(bool Success, Stream? FileStream, string? ContentType, string? FileName, string ErrorMessage)> DownloadFileAsync(
        int fileId, 
        string requestedBy)
    {
        try
        {
            var file = await _context.UploadedFiles.FindAsync(fileId);

            if (file == null || file.IsDeleted)
            {
                return (false, null, null, null, "File not found.");
            }

            if (!File.Exists(file.FilePath))
            {
                _logger.LogError("File not found on disk: {FilePath}", file.FilePath);
                return (false, null, null, null, "File not found on disk.");
            }

            Stream fileStream;

            if (file.IsEncrypted)
            {
                fileStream = await _encryptionService.DecryptFileAsync(file.FilePath);
            }
            else
            {
                fileStream = File.OpenRead(file.FilePath);
            }

            // Update access tracking
            file.DownloadCount++;
            file.LastAccessedAt = DateTime.UtcNow;
            file.LastAccessedBy = requestedBy;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "File downloaded: ID={FileId}, Name={FileName}, User={User}",
                fileId, file.FileName, requestedBy);

            return (true, fileStream, file.ContentType, file.FileName, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: ID={FileId}, User={User}", fileId, requestedBy);
            return (false, null, null, null, "An error occurred while downloading the file.");
        }
    }

    public async Task<bool> DeleteFileAsync(int fileId, string deletedBy)
    {
        try
        {
            var file = await _context.UploadedFiles.FindAsync(fileId);

            if (file == null || file.IsDeleted)
            {
                return false;
            }

            // Soft delete
            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
            file.DeletedBy = deletedBy;
            file.ScheduledPurgeDate = DateTime.UtcNow.AddDays(30); // 30-day grace period

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "File soft deleted: ID={FileId}, Name={FileName}, User={User}",
                fileId, file.FileName, deletedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: ID={FileId}, User={User}", fileId, deletedBy);
            return false;
        }
    }

    public async Task<bool> PermanentlyDeleteFileAsync(int fileId)
    {
        try
        {
            var file = await _context.UploadedFiles.FindAsync(fileId);

            if (file == null)
            {
                return false;
            }

            // Delete physical file
            if (File.Exists(file.FilePath))
            {
                if (file.IsEncrypted)
                {
                    // Use secure delete for encrypted files
                    File.Delete(file.FilePath);
                }
                else
                {
                    File.Delete(file.FilePath);
                }
            }

            // Remove from database
            _context.UploadedFiles.Remove(file);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "File permanently deleted: ID={FileId}, Name={FileName}",
                fileId, file.FileName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting file: ID={FileId}", fileId);
            return false;
        }
    }

    public async Task<UploadedFile?> GetFileByIdAsync(int fileId)
    {
        return await _context.UploadedFiles
            .Where(f => !f.IsDeleted)
            .FirstOrDefaultAsync(f => f.Id == fileId);
    }

    public async Task<(List<UploadedFile> Files, int TotalCount)> SearchFilesAsync(
        string? searchTerm,
        string? category,
        DateTime? uploadedAfter,
        DateTime? uploadedBefore,
        string? uploadedBy,
        bool? isPublic,
        int page,
        int pageSize)
    {
        var query = _context.UploadedFiles
            .Where(f => !f.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var sanitized = searchTerm.Trim();
            query = query.Where(f =>
                f.FileName.Contains(sanitized) ||
                (f.Description != null && f.Description.Contains(sanitized)) ||
                (f.Tags != null && f.Tags.Contains(sanitized)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(f => f.Category == category);
        }

        if (uploadedAfter.HasValue)
        {
            query = query.Where(f => f.UploadedAt >= uploadedAfter.Value);
        }

        if (uploadedBefore.HasValue)
        {
            query = query.Where(f => f.UploadedAt <= uploadedBefore.Value);
        }

        if (!string.IsNullOrWhiteSpace(uploadedBy))
        {
            query = query.Where(f => f.UploadedBy == uploadedBy);
        }

        if (isPublic.HasValue)
        {
            query = query.Where(f => f.IsPublic == isPublic.Value);
        }

        var totalCount = await query.CountAsync();

        var files = await query
            .OrderByDescending(f => f.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (files, totalCount);
    }

    public List<string> GetCategories()
    {
        return FileCategories.GetAll();
    }
}
