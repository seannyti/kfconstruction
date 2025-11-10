using System.Text;

namespace KfConstructionWeb.Helpers;

/// <summary>
/// File validation helper for secure file upload handling
/// OWASP ASVS L2 compliant file validation
/// </summary>
public static class FileValidationHelper
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars()
        .Concat(new[] { '\0', '\u200B', '\uFEFF' }) // null byte, zero-width space, BOM
        .ToArray();

    private const int MaxFileNameLength = 200;

    /// <summary>
    /// Validates and sanitizes an uploaded file name
    /// </summary>
    /// <param name="fileName">Original file name from upload</param>
    /// <param name="allowedExtensions">Array of allowed file extensions (e.g., [".jpg", ".png"])</param>
    /// <returns>Tuple with validation result, error message, and secure file name</returns>
    public static (bool IsValid, string? ErrorMessage, string SecureFileName) ValidateAndSanitizeFileName(
        string fileName, 
        string[] allowedExtensions)
    {
        // Check if file name is provided
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return (false, "File name is required.", string.Empty);
        }

        // Remove null bytes and zero-width characters (security: prevent null byte injection)
        fileName = fileName
            .Replace("\0", "")
            .Replace("\u200B", "")
            .Replace("\uFEFF", "");

        // Get sanitized file name (removes path information)
        var sanitizedName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return (false, "Invalid file name.", string.Empty);
        }

        // Check file name length (security: prevent buffer overflow, DoS)
        if (sanitizedName.Length > MaxFileNameLength)
        {
            return (false, $"File name too long (maximum {MaxFileNameLength} characters).", string.Empty);
        }

        // Check for path traversal attempts (security: prevent directory traversal)
        if (sanitizedName.Contains("..") || 
            sanitizedName.Contains("/") || 
            sanitizedName.Contains("\\"))
        {
            return (false, "Invalid path characters detected in file name.", string.Empty);
        }

        // Check for invalid file name characters (security: prevent file system attacks)
        if (sanitizedName.Any(c => InvalidFileNameChars.Contains(c)))
        {
            return (false, "File name contains invalid characters.", string.Empty);
        }

        // Validate file extension (security: prevent execution of malicious files)
        var extension = Path.GetExtension(sanitizedName).ToLowerInvariant();
        
        if (string.IsNullOrEmpty(extension))
        {
            return (false, "File must have an extension.", string.Empty);
        }

        if (!allowedExtensions.Contains(extension))
        {
            return (false, 
                $"File type '{extension}' not allowed. Allowed types: {string.Join(", ", allowedExtensions)}", 
                string.Empty);
        }

        // Generate cryptographically secure file name (security: prevent enumeration attacks)
        var secureFileName = GenerateSecureFileName(extension);

        return (true, null, secureFileName);
    }

    /// <summary>
    /// Generates a cryptographically secure random file name
    /// </summary>
    /// <param name="extension">File extension to append</param>
    /// <returns>Secure file name with timestamp and random component</returns>
    public static string GenerateSecureFileName(string extension)
    {
        // Use GUID for uniqueness and timestamp for sortability
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var guid = Guid.NewGuid().ToString("N"); // N format = 32 hex digits without hyphens
        
        return $"{timestamp}_{guid}{extension}";
    }

    /// <summary>
    /// Validates file size against maximum allowed size
    /// </summary>
    /// <param name="fileSize">Size in bytes</param>
    /// <param name="maxSizeBytes">Maximum allowed size in bytes</param>
    /// <returns>Tuple with validation result and error message</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateFileSize(long fileSize, long maxSizeBytes)
    {
        if (fileSize <= 0)
        {
            return (false, "File is empty or invalid.");
        }

        if (fileSize > maxSizeBytes)
        {
            var maxSizeMB = maxSizeBytes / (1024.0 * 1024.0);
            return (false, $"File size exceeds maximum allowed size of {maxSizeMB:F1} MB.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates content type against MIME type
    /// </summary>
    /// <param name="contentType">Content type from upload</param>
    /// <param name="extension">File extension</param>
    /// <returns>Tuple with validation result and error message</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateContentType(string contentType, string extension)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return (false, "Content type is missing.");
        }

        // Map of extensions to allowed MIME types
        var allowedMimeTypes = new Dictionary<string, string[]>
        {
            { ".jpg", new[] { "image/jpeg", "image/pjpeg" } },
            { ".jpeg", new[] { "image/jpeg", "image/pjpeg" } },
            { ".png", new[] { "image/png" } },
            { ".pdf", new[] { "application/pdf" } },
            { ".gif", new[] { "image/gif" } },
            { ".bmp", new[] { "image/bmp", "image/x-windows-bmp" } }
        };

        var extensionLower = extension.ToLowerInvariant();
        
        if (!allowedMimeTypes.ContainsKey(extensionLower))
        {
            return (false, $"Extension '{extension}' not recognized.");
        }

        var allowedTypes = allowedMimeTypes[extensionLower];
        var contentTypeLower = contentType.ToLowerInvariant();

        if (!allowedTypes.Any(t => contentTypeLower.Contains(t)))
        {
            return (false, 
                $"Content type '{contentType}' does not match file extension '{extension}'.");
        }

        return (true, null);
    }

    /// <summary>
    /// Comprehensive file upload validation
    /// </summary>
    /// <param name="fileName">Original file name</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="allowedExtensions">Allowed file extensions</param>
    /// <param name="maxSizeBytes">Maximum file size in bytes</param>
    /// <returns>Tuple with validation result, error message, and secure file name</returns>
    public static (bool IsValid, string? ErrorMessage, string SecureFileName) ValidateFileUpload(
        string fileName,
        long fileSize,
        string contentType,
        string[] allowedExtensions,
        long maxSizeBytes)
    {
        // Validate file name and get secure name
        var (nameValid, nameError, secureName) = ValidateAndSanitizeFileName(fileName, allowedExtensions);
        if (!nameValid)
        {
            return (false, nameError, string.Empty);
        }

        // Validate file size
        var (sizeValid, sizeError) = ValidateFileSize(fileSize, maxSizeBytes);
        if (!sizeValid)
        {
            return (false, sizeError, string.Empty);
        }

        // Validate content type matches extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var (typeValid, typeError) = ValidateContentType(contentType, extension);
        if (!typeValid)
        {
            return (false, typeError, string.Empty);
        }

        return (true, null, secureName);
    }
}
