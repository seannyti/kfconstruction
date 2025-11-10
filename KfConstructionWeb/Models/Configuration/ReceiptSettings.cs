namespace KfConstructionWeb.Models.Configuration;

/// <summary>
/// Configuration for receipt management system
/// </summary>
public class ReceiptSettings
{
    /// <summary>
    /// Azure AI Document Intelligence endpoint
    /// </summary>
    public string AzureFormRecognizerEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure AI Document Intelligence API key
    /// </summary>
    public string AzureFormRecognizerApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Maximum file size in bytes (default 10MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed file extensions
    /// </summary>
    public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".pdf" };

    /// <summary>
    /// Data retention period in months (default 84 months = 7 years for tax purposes)
    /// </summary>
    public int RetentionPeriodMonths { get; set; } = 84;

    /// <summary>
    /// AES encryption key for at-rest file encryption (should be in Key Vault in production)
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Target p95 latency in milliseconds
    /// </summary>
    public int TargetP95LatencyMs { get; set; } = 200;

    /// <summary>
    /// Enable automated backup verification
    /// </summary>
    public bool EnableBackupVerification { get; set; } = true;

    /// <summary>
    /// Backup verification schedule (cron expression)
    /// </summary>
    public string BackupVerificationSchedule { get; set; } = "0 2 * * *"; // Daily at 2 AM

    /// <summary>
    /// Target SLO availability percentage
    /// </summary>
    public double TargetSloPercentage { get; set; } = 99.9;
}
