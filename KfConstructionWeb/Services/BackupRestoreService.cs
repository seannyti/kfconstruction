using KfConstructionWeb.Models;
using KfConstructionWeb.Models.ViewModels;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KfConstructionWeb.Services;

/// <summary>
/// Service for SQL Server database backup and restore operations
/// </summary>
public class BackupRestoreService : IBackupRestoreService
{
    private readonly IConfiguration _configuration;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<BackupRestoreService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _backupDirectory;
    private readonly bool _isAzureSql;

    public BackupRestoreService(
        IConfiguration configuration,
        IActivityLogService activityLogService,
        ILogger<BackupRestoreService> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));

        _backupDirectory = Path.Combine(_environment.ContentRootPath, "Backups");
        Directory.CreateDirectory(_backupDirectory);

        // Detect if using Azure SQL Database
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        _isAzureSql = connectionString?.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task<(bool Success, string? BackupPath, string ErrorMessage)> CreateBackupAsync(string performedBy)
    {
        // Azure SQL Database doesn't support BACKUP/RESTORE commands
        // Use Azure Portal, Azure CLI, or automated backups instead
        if (_isAzureSql)
        {
            var message = "Azure SQL Database backups are managed by Azure. Use the Azure Portal to configure and restore backups:\n" +
                         "1. Go to Azure Portal > Your SQL Database\n" +
                         "2. Click 'Restore' to restore from automatic backups (up to 35 days retention)\n" +
                         "3. Use 'Export' to create a BACPAC file for manual backups\n" +
                         "Point-in-time restore is available for up to 35 days.";
            
            _logger.LogWarning("Backup operation attempted on Azure SQL Database. Redirecting to Azure Portal.");
            return (false, null, message);
        }

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{databaseName}_Backup_{timestamp}.bak";
            var backupPath = Path.Combine(_backupDirectory, backupFileName);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var backupQuery = $@"
                BACKUP DATABASE [{databaseName}] 
                TO DISK = @BackupPath 
                WITH FORMAT, INIT, 
                NAME = N'{databaseName}-Full Database Backup', 
                SKIP, NOREWIND, NOUNLOAD, STATS = 10";

            using var command = new SqlCommand(backupQuery, connection);
            command.CommandTimeout = 300; // 5 minutes timeout
            command.Parameters.AddWithValue("@BackupPath", backupPath);

            _logger.LogInformation("Starting database backup: {DatabaseName} to {BackupPath}", databaseName, backupPath);
            await command.ExecuteNonQueryAsync();

            await _activityLogService.LogActivityAsync(
                ActivityCategories.System,
                "Database Backup Created",
                performedBy,
                $"Database backup created: {backupFileName}",
                "Database",
                null,
                ActivitySeverity.Info);

            _logger.LogInformation("Database backup completed successfully: {BackupPath}", backupPath);
            return (true, backupPath, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database backup");

            await _activityLogService.LogActivityAsync(
                ActivityCategories.System,
                "Database Backup Failed",
                performedBy,
                $"Backup failed: {ex.Message}",
                "Database",
                null,
                ActivitySeverity.Error);

            return (false, null, $"Failed to create backup: {ex.Message}");
        }
    }

    public async Task<(bool Success, string ErrorMessage)> RestoreDatabaseAsync(string backupFilePath, string performedBy)
    {
        // Azure SQL Database doesn't support BACKUP/RESTORE commands
        if (_isAzureSql)
        {
            var message = "Azure SQL Database restore must be done through Azure Portal:\n" +
                         "1. Go to Azure Portal > Your SQL Database\n" +
                         "2. Click 'Restore' and select a point in time (up to 35 days)\n" +
                         "3. Or use 'Import' to restore from a BACPAC file\n" +
                         "Note: Restore creates a new database - you'll need to update connection strings.";
            
            _logger.LogWarning("Restore operation attempted on Azure SQL Database. Redirecting to Azure Portal.");
            return (false, message);
        }

        try
        {
            if (!File.Exists(backupFilePath))
            {
                return (false, "Backup file not found.");
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;

            // Switch to master database for restore
            builder.InitialCatalog = "master";
            var masterConnectionString = builder.ConnectionString;

            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();

            // Set database to single user mode and restore
            var restoreQuery = $@"
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                
                RESTORE DATABASE [{databaseName}] 
                FROM DISK = @BackupPath 
                WITH REPLACE, STATS = 10;
                
                ALTER DATABASE [{databaseName}] SET MULTI_USER;";

            using var command = new SqlCommand(restoreQuery, connection);
            command.CommandTimeout = 600; // 10 minutes timeout
            command.Parameters.AddWithValue("@BackupPath", backupFilePath);

            _logger.LogWarning("Starting database restore: {DatabaseName} from {BackupPath}", databaseName, backupFilePath);
            await command.ExecuteNonQueryAsync();

            await _activityLogService.LogActivityAsync(
                ActivityCategories.System,
                "Database Restored",
                performedBy,
                $"Database restored from: {Path.GetFileName(backupFilePath)}",
                "Database",
                null,
                ActivitySeverity.Warning);

            _logger.LogInformation("Database restore completed successfully");
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database");

            await _activityLogService.LogActivityAsync(
                ActivityCategories.System,
                "Database Restore Failed",
                performedBy,
                $"Restore failed: {ex.Message}",
                "Database",
                null,
                ActivitySeverity.Critical);

            return (false, $"Failed to restore database: {ex.Message}");
        }
    }

    public async Task<List<BackupInfo>> GetAvailableBackupsAsync()
    {
        return await Task.Run(() =>
        {
            var backups = new List<BackupInfo>();

            if (!Directory.Exists(_backupDirectory))
            {
                return backups;
            }

            var backupFiles = Directory.GetFiles(_backupDirectory, "*.bak")
                .OrderByDescending(f => File.GetCreationTime(f));

            foreach (var file in backupFiles)
            {
                var fileInfo = new FileInfo(file);
                backups.Add(new BackupInfo
                {
                    FileName = fileInfo.Name,
                    FilePath = file,
                    CreatedDate = fileInfo.CreationTime,
                    FileSizeBytes = fileInfo.Length
                });
            }

            return backups;
        });
    }

    public async Task<bool> DeleteBackupAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_backupDirectory, fileName);

            if (!File.Exists(filePath))
            {
                return false;
            }

            await Task.Run(() => File.Delete(filePath));
            _logger.LogInformation("Deleted backup file: {FileName}", fileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup file: {FileName}", fileName);
            return false;
        }
    }

    public async Task<long> GetDatabaseSizeBytesAsync()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Azure SQL Database compatible query - sys.master_files is not available in Azure SQL
            // Use sys.database_files instead which works for both Azure SQL and SQL Server
            var query = @"
                SELECT SUM(CAST(size AS bigint)) * 8 * 1024 AS DatabaseSizeBytes
                FROM sys.database_files";

            using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();

            return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database size");
            return 0;
        }
    }

    public async Task<(bool Success, Stream? FileStream, string? FileName, string ErrorMessage)> DownloadBackupAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_backupDirectory, fileName);

            if (!File.Exists(filePath))
            {
                return (false, null, null, "Backup file not found.");
            }

            var fileStream = await Task.Run(() => File.OpenRead(filePath));
            return (true, fileStream, fileName, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading backup file: {FileName}", fileName);
            return (false, null, null, $"Error downloading backup: {ex.Message}");
        }
    }
}
