namespace KfConstructionWeb.Models;

/// <summary>
/// Result of OCR extraction for receipts
/// </summary>
public class OcrResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double Confidence { get; set; }
    public string? ReceiptNumber { get; set; }
    public string Vendor { get; set; } = "Unknown";
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string? CardLastFour { get; set; }
    public string PaymentMethod { get; set; } = PaymentMethods.Unknown;
    public string? RawText { get; set; }
}
