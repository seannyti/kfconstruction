using KfConstructionWeb.Models;

namespace KfConstructionWeb.Services.Interfaces;

public interface IReceiptOcrService
{
    /// <summary>
    /// Extracts receipt data from an image or PDF using Azure AI Document Intelligence
    /// Target: p95 latency < 200ms
    /// </summary>
    Task<OcrResult> ExtractReceiptDataAsync(Stream fileStream, string contentType);

    /// <summary>
    /// Validates OCR results for accuracy and completeness
    /// </summary>
    bool ValidateOcrResults(OcrResult result);
}
