namespace KfConstructionWeb.Services.Interfaces;

/// <summary>
/// Service for database backup and restore operations
/// </summary>
public interface IBackupRestoreService
{
    /// <summary>
    /// Create a database backup
    /// </summary>
    Task<(bool Success, string? BackupPath, string ErrorMessage)> CreateBackupAsync(string performedBy);

    /// <summary>
    /// Restore database from backup file
    /// </summary>
    Task<(bool Success, string ErrorMessage)> RestoreDatabaseAsync(string backupFilePath, string performedBy);

    /// <summary>
    /// Get list of available backups
    /// </summary>
    Task<List<Models.ViewModels.BackupInfo>> GetAvailableBackupsAsync();

    /// <summary>
    /// Delete a backup file
    /// </summary>
    Task<bool> DeleteBackupAsync(string fileName);

    /// <summary>
    /// Get database size
    /// </summary>
    Task<long> GetDatabaseSizeBytesAsync();

    /// <summary>
    /// Download backup file
    /// </summary>
    Task<(bool Success, Stream? FileStream, string? FileName, string ErrorMessage)> DownloadBackupAsync(string fileName);
}
