using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using KfConstructionWeb.Models; // OcrResult & PaymentMethods
using KfConstructionWeb.Services.Interfaces;
using System.Diagnostics;

namespace KfConstructionWeb.Services;

/// <summary>
/// OCR service using Azure AI Document Intelligence (formerly Form Recognizer)
/// Performance optimized for p95 < 200ms at expected scale
/// </summary>
public class ReceiptOcrService : IReceiptOcrService
{
    private readonly DocumentAnalysisClient _client;
    private readonly ILogger<ReceiptOcrService> _logger;
    private readonly IConfiguration _configuration;

    public ReceiptOcrService(ILogger<ReceiptOcrService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var endpoint = _configuration["ReceiptSettings:AzureFormRecognizerEndpoint"];
        var apiKey = _configuration["ReceiptSettings:AzureFormRecognizerApiKey"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Azure Form Recognizer not configured. OCR will be disabled.");
            _client = null!;
        }
        else
        {
            var credential = new AzureKeyCredential(apiKey);
            _client = new DocumentAnalysisClient(new Uri(endpoint), credential);
        }
    }

    /// <summary>
    /// Extracts receipt data using Azure AI Document Intelligence
    /// Target: p95 < 200ms
    /// </summary>
    public async Task<OcrResult> ExtractReceiptDataAsync(Stream fileStream, string contentType)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new OcrResult();

        try
        {
            if (_client == null)
            {
                _logger.LogWarning("Azure Form Recognizer not configured. Skipping OCR.");
                result.Success = false;
                result.ErrorMessage = "OCR service not configured";
                return result;
            }

            _logger.LogInformation("Starting OCR processing for receipt (ContentType: {ContentType})", contentType);

            // Use prebuilt receipt model from Azure
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-receipt",
                fileStream);

            stopwatch.Stop();
            var latencyMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("OCR processing completed in {Latency}ms", latencyMs);

            // Track performance metrics
            if (latencyMs > 200)
            {
                _logger.LogWarning("OCR latency exceeded target: {Latency}ms > 200ms", latencyMs);
            }

            var receiptAnalysis = operation.Value;

            if (receiptAnalysis.Documents.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No receipt detected in image";
                return result;
            }

            var receipt = receiptAnalysis.Documents[0];
            result.Confidence = receipt.Confidence;
            result.Success = true;

            // Log all available fields for debugging
            _logger.LogDebug("Available OCR fields: {Fields}", 
                string.Join(", ", receipt.Fields.Keys));
            
            // Extract merchant name
            if (receipt.Fields.TryGetValue("MerchantName", out var merchantName) && merchantName != null)
            {
                result.Vendor = merchantName.Content ?? "Unknown";
                _logger.LogDebug("Extracted Vendor: {Vendor} (Confidence: {Confidence})", 
                    result.Vendor, merchantName.Confidence);
            }

            // Extract transaction date
            if (receipt.Fields.TryGetValue("TransactionDate", out var transactionDate) && transactionDate != null)
            {
                if (transactionDate.FieldType == DocumentFieldType.Date)
                {
                    var dateValue = transactionDate.Value.AsDate();
                    result.PurchaseDate = dateValue.DateTime;
                    _logger.LogDebug("Extracted Date: {Date} (Confidence: {Confidence})", 
                        result.PurchaseDate, transactionDate.Confidence);
                }
            }

            // Extract total amount
            if (receipt.Fields.TryGetValue("Total", out var total) && total != null)
            {
                if (total.FieldType == DocumentFieldType.Currency)
                {
                    var currencyValue = total.Value.AsCurrency();
                    result.TotalAmount = (decimal)currencyValue.Amount;
                    _logger.LogDebug("Extracted Amount: {Amount} (Confidence: {Confidence})", 
                        result.TotalAmount, total.Confidence);
                }
            }
            
            // Fallback: Try alternative total fields if main Total not found
            if (result.TotalAmount == 0)
            {
                var totalFields = new[] { "AmountDue", "SubTotal", "TotalPrice", "GrandTotal" };
                foreach (var fieldName in totalFields)
                {
                    if (receipt.Fields.TryGetValue(fieldName, out var altTotal) && altTotal != null 
                        && altTotal.FieldType == DocumentFieldType.Currency)
                    {
                        var currencyValue = altTotal.Value.AsCurrency();
                        result.TotalAmount = (decimal)currencyValue.Amount;
                        _logger.LogDebug("Extracted Amount from {Field}: {Amount}", fieldName, result.TotalAmount);
                        break;
                    }
                }
            }

            // Extract receipt number (if available)
            if (receipt.Fields.TryGetValue("ReceiptNumber", out var receiptNumber) && receiptNumber != null)
            {
                result.ReceiptNumber = receiptNumber.Content;
                _logger.LogDebug("Extracted Receipt Number: {Number}", result.ReceiptNumber);
            }

            // Extract payment method (if available)
            if (receipt.Fields.TryGetValue("PaymentMethod", out var paymentMethod) && paymentMethod != null)
            {
                result.PaymentMethod = paymentMethod.Content ?? PaymentMethods.Unknown;
            }

            // Extract last 4 digits of card (if available)
            if (receipt.Fields.TryGetValue("Last4", out var last4) && last4 != null)
            {
                result.CardLastFour = last4.Content;
            }
            
            // Try to extract card info from other fields
            if (string.IsNullOrEmpty(result.CardLastFour))
            {
                var cardFields = new[] { "CreditCardLast4", "CardNumber", "AccountNumber" };
                foreach (var fieldName in cardFields)
                {
                    if (receipt.Fields.TryGetValue(fieldName, out var cardField) && cardField != null)
                    {
                        var cardContent = cardField.Content ?? "";
                        // Extract last 4 digits from various formats like "****1234" or "XXXX-XXXX-XXXX-1234"
                        var digits = new string(cardContent.Where(char.IsDigit).ToArray());
                        if (digits.Length >= 4)
                        {
                            result.CardLastFour = digits.Substring(digits.Length - 4);
                            _logger.LogDebug("Extracted Last4 from {Field}: {Last4}", fieldName, result.CardLastFour);
                            break;
                        }
                    }
                }
            }

            // Build raw OCR text from all detected text
            var rawTextBuilder = new System.Text.StringBuilder();
            foreach (var page in receiptAnalysis.Pages)
            {
                foreach (var line in page.Lines)
                {
                    rawTextBuilder.AppendLine(line.Content);
                }
            }
            result.RawText = rawTextBuilder.ToString();
            
            // Last resort: Parse total from raw text if structured fields failed
            if (result.TotalAmount == 0)
            {
                result.TotalAmount = ExtractTotalFromText(result.RawText);
                if (result.TotalAmount > 0)
                {
                    _logger.LogDebug("Extracted Amount from raw text: {Amount}", result.TotalAmount);
                }
            }
            
            // Intelligent payment method detection if not found
            if (string.IsNullOrEmpty(result.PaymentMethod) || result.PaymentMethod == PaymentMethods.Unknown)
            {
                result.PaymentMethod = DetectPaymentMethod(result.RawText, result.CardLastFour);
                _logger.LogDebug("Detected Payment Method: {PaymentMethod}", result.PaymentMethod);
            }

            _logger.LogInformation(
                "OCR extraction successful: Vendor={Vendor}, Amount={Amount}, Date={Date}, Confidence={Confidence}",
                result.Vendor, result.TotalAmount, result.PurchaseDate, result.Confidence);

            return result;
        }
        catch (RequestFailedException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Azure Form Recognizer API error: {Message}", ex.Message);
            result.Success = false;
            result.ErrorMessage = $"OCR API error: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during OCR processing");
            result.Success = false;
            result.ErrorMessage = $"OCR processing failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Validates that OCR results contain minimum required data
    /// </summary>
    public bool ValidateOcrResults(OcrResult result)
    {
        if (!result.Success)
            return false;

        var hasVendor = !string.IsNullOrWhiteSpace(result.Vendor);
        var hasAmount = result.TotalAmount > 0;
        var hasDate = result.PurchaseDate != default;
        var hasMinimumConfidence = result.Confidence >= 0.5; // At least 50% confidence

        var isValid = hasVendor && hasAmount && hasDate && hasMinimumConfidence;

        if (!isValid)
        {
            _logger.LogWarning(
                "OCR validation failed: Vendor={HasVendor}, Amount={HasAmount}, Date={HasDate}, Confidence={Confidence}",
                hasVendor, hasAmount, hasDate, result.Confidence);
        }

        return isValid;
    }

    /// <summary>
    /// Extracts total amount from raw OCR text using pattern matching
    /// </summary>
    private decimal ExtractTotalFromText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return 0;

        // Common patterns for total amounts on receipts (ordered by priority)
        var patterns = new[]
        {
            @"(?:^|\s)TOTAL(?!\s*SUB)[:\s]*\$?\s*(\d+\.\d{2})",    // "TOTAL: $12.34" but NOT "TOTAL SUB"
            @"GRAND\s*TOTAL[:\s]*\$?\s*(\d+\.\d{2})",              // "GRAND TOTAL: $12.34"
            @"FINAL\s*TOTAL[:\s]*\$?\s*(\d+\.\d{2})",              // "FINAL TOTAL: $12.34"
            @"AMOUNT\s*DUE[:\s]*\$?\s*(\d+\.\d{2})",               // "AMOUNT DUE: $12.34"
            @"BALANCE[:\s]*DUE[:\s]*\$?\s*(\d+\.\d{2})",           // "BALANCE DUE: $12.34"
            @"\$\s*(\d+\.\d{2})\s*USD",                            // "$12.34 USD"
            @"USD\s*\$?\s*(\d+\.\d{2})"                            // "USD $12.34" or "USD 12.34"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                rawText, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var amount))
            {
                _logger.LogDebug("Extracted amount using pattern '{Pattern}': {Amount}", pattern, amount);
                return amount;
            }
        }

        return 0;
    }

    /// <summary>
    /// Intelligently detects payment method from receipt text
    /// </summary>
    private string DetectPaymentMethod(string rawText, string? cardLastFour)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return PaymentMethods.Unknown;

        var text = rawText.ToUpperInvariant();

        // If we have card last 4 digits, it's likely a card payment
        if (!string.IsNullOrEmpty(cardLastFour))
        {
            // Check for debit indicators
            if (text.Contains("DEBIT") || text.Contains("DB ") || text.Contains("PIN"))
                return PaymentMethods.DebitCard;
            
            // Otherwise assume credit card
            return PaymentMethods.CreditCard;
        }

        // Check for specific payment methods in text
        if (text.Contains("CASH") || text.Contains("CHANGE DUE") || text.Contains("TENDERED"))
            return PaymentMethods.Cash;

        if (text.Contains("CHECK") || text.Contains("CHEQUE"))
            return PaymentMethods.Check;

        if (text.Contains("WIRE") || text.Contains("TRANSFER") || text.Contains("ACH"))
            return PaymentMethods.BankTransfer;

        if (text.Contains("VISA") || text.Contains("MASTERCARD") || text.Contains("MASTER CARD") ||
            text.Contains("AMEX") || text.Contains("AMERICAN EXPRESS") || text.Contains("DISCOVER"))
        {
            // Check if it's debit or credit
            if (text.Contains("DEBIT"))
                return PaymentMethods.DebitCard;
            return PaymentMethods.CreditCard;
        }

        // Check for card-like patterns (numbers ending in 4 digits)
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\*+\d{4}|\d{4}\s*$"))
        {
            if (text.Contains("DEBIT"))
                return PaymentMethods.DebitCard;
            return PaymentMethods.CreditCard;
        }

        return PaymentMethods.Unknown;
    }
}

