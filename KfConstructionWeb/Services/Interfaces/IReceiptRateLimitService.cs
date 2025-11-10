namespace KfConstructionWeb.Services.Interfaces;

public interface IReceiptRateLimitService
{
    /// <summary>
    /// Checks if an IP address can upload a receipt
    /// </summary>
    Task<ReceiptRateLimitResult> CheckRateLimitAsync(string ipAddress);

    /// <summary>
    /// Records a successful upload for rate limiting
    /// </summary>
    Task RecordUploadAsync(string ipAddress);

    /// <summary>
    /// Gets current upload count for an IP address
    /// </summary>
    Task<int> GetUploadCountAsync(string ipAddress);
}
