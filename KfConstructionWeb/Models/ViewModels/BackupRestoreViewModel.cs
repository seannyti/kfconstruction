namespace KfConstructionWeb.Models.ViewModels;

public class BackupRestoreViewModel
{
    public List<BackupInfo> AvailableBackups { get; set; } = new();
    public long DatabaseSizeBytes { get; set; }
    public string DatabaseSizeFormatted => UploadedFile.FormatFileSize(DatabaseSizeBytes);
    public DateTime? LastBackupDate { get; set; }
    public bool BackupInProgress { get; set; }
    public bool RestoreInProgress { get; set; }
}

public class BackupInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted => UploadedFile.FormatFileSize(FileSizeBytes);
}
