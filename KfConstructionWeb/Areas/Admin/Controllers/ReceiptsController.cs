using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;
using KfConstructionWeb.Helpers;
using KfConstructionWeb.Controllers;
using System.Diagnostics;
using System.Text.Encodings.Web;

namespace KfConstructionWeb.Areas.Admin.Controllers;

/// <summary>
/// Controller for receipt management with OWASP ASVS L2 security compliance
/// Includes: Input validation, CSRF protection, authorization, audit logging
/// Performance target: p95 < 200ms
/// Security: Inherits common security utilities from BaseAdminController
/// </summary>
public class ReceiptsController : BaseAdminController
{
    private readonly ApplicationDbContext _context;
    private readonly IReceiptOcrService _ocrService;
    private readonly IFileEncryptionService _encryptionService;
    private readonly IReceiptRateLimitService _rateLimitService;
    private readonly ILogger<ReceiptsController> _logger;
    private readonly IConfiguration _configuration;

    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;

    public ReceiptsController(
        ApplicationDbContext context,
        IReceiptOcrService ocrService,
        IFileEncryptionService encryptionService,
        IReceiptRateLimitService rateLimitService,
        ILogger<ReceiptsController> logger,
        IConfiguration configuration)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _rateLimitService = rateLimitService ?? throw new ArgumentNullException(nameof(rateLimitService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _maxFileSize = _configuration.GetValue<long>("ReceiptSettings:MaxFileSizeBytes", 10 * 1024 * 1024);
        _allowedExtensions = _configuration.GetSection("ReceiptSettings:AllowedExtensions").Get<string[]>()
            ?? new[] { ".jpg", ".jpeg", ".png", ".pdf" };
    }

    /// <summary>
    /// Display receipts list with search and filtering
    /// Security: Authorization required, XSS protection, SQL injection prevention
    /// Performance: Indexed queries, pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        ExpenseCategory? category,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1,
        int pageSize = 20)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Input validation and sanitization
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);

            // Build query with proper indexing
            var query = _context.Receipts
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            // Apply filters (using parameterized queries to prevent SQL injection)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var sanitized = searchTerm.Trim();
                query = query.Where(r =>
                    r.Vendor.Contains(sanitized) ||
                    (r.ReceiptNumber != null && r.ReceiptNumber.Contains(sanitized)) ||
                    (r.Notes != null && r.Notes.Contains(sanitized)));
            }

            if (category.HasValue)
            {
                query = query.Where(r => r.Category == category.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.PurchaseDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.PurchaseDate <= endDate.Value);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Execute query with pagination
            var receipts = await query
                .OrderByDescending(r => r.PurchaseDate)
                .ThenByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            stopwatch.Stop();

            // Track performance metrics
            _logger.LogInformation(
                "Receipts list loaded: {Count} records, {Latency}ms (User: {User})",
                receipts.Count, stopwatch.ElapsedMilliseconds, GetCurrentUserId());

            if (stopwatch.ElapsedMilliseconds > 200)
            {
                _logger.LogWarning("Query exceeded p95 target: {Latency}ms", stopwatch.ElapsedMilliseconds);
            }

            // Pass data to view
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Category = category;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Categories = Enum.GetValues<ExpenseCategory>();

            return View(receipts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading receipts list (User: {User})", GetCurrentUserId());
            SetErrorMessage("An error occurred while loading receipts.");
            return View(new List<Receipt>());
        }
    }

    /// <summary>
    /// Display upload form
    /// Security: CSRF token required
    /// </summary>
    [HttpGet]
    public IActionResult Upload()
    {
        PopulateReceiptFormData();
        return View(new Receipt());
    }

    /// <summary>
    /// Handle receipt upload with OCR processing
    /// Security: CSRF protection, file validation, input sanitization, virus scanning recommended
    /// Performance: Async processing, p95 < 200ms target
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)] // 30MB limit for receipt uploads
    public async Task<IActionResult> Upload(Receipt model, IFormFile receiptFile)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUser = GetCurrentUserId();

        _logger.LogWarning("=== RECEIPT UPLOAD STARTED === User: {User}, HasFile: {HasFile}, FileSize: {Size}", 
            currentUser, receiptFile != null, receiptFile?.Length ?? 0);

        try
        {
            // Security: Rate limiting check (prevent DoS attacks)
            var clientIp = GetClientIpAddress();
            var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(clientIp);
            
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for receipt upload: IP={IpAddress}, Attempts={Attempts}/{Max}",
                    clientIp, rateLimitResult.AttemptsInWindow, rateLimitResult.MaxAllowed);
                    
                ModelState.AddModelError(string.Empty, rateLimitResult.Message);
                PopulateReceiptFormData();
                return View("Upload", model);
            }

            // Validate file upload
            if (receiptFile == null || receiptFile.Length == 0)
            {
                ModelState.AddModelError(nameof(receiptFile), "Please select a file to upload.");
                PopulateReceiptFormData();
                return View("Upload", model);
            }

            // Security: Comprehensive file validation using helper
            var (isValid, errorMessage, secureFileName) = FileValidationHelper.ValidateFileUpload(
                receiptFile.FileName,
                receiptFile.Length,
                receiptFile.ContentType,
                _allowedExtensions,
                _maxFileSize);

            if (!isValid)
            {
                _logger.LogWarning(
                    "File validation failed: {Error}, FileName: {FileName}, User: {User}",
                    errorMessage, receiptFile.FileName, currentUser);
                ModelState.AddModelError(nameof(receiptFile), errorMessage!);
                PopulateReceiptFormData();
                return View("Upload", model);
            }

            _logger.LogInformation("Processing receipt upload: {FileName} ({Size} bytes) by {User}",
                receiptFile.FileName, receiptFile.Length, currentUser);

            // Step 1: OCR Processing
            OcrResult ocrResult;
            using (var stream = receiptFile.OpenReadStream())
            {
                ocrResult = await _ocrService.ExtractReceiptDataAsync(stream, receiptFile.ContentType);
            }

            // Security: Validate and sanitize OCR results
            if (ocrResult.Success)
            {
                // Validate OCR confidence and completeness
                if (!_ocrService.ValidateOcrResults(ocrResult))
                {
                    _logger.LogWarning(
                        "OCR validation failed: Low confidence ({Confidence}) or missing required fields",
                        ocrResult.Confidence);
                    ocrResult.Success = false;
                    ocrResult.ErrorMessage = "Receipt data could not be extracted reliably. Please enter manually.";
                }

                // Sanitize OCR text to prevent XSS attacks
                if (!string.IsNullOrWhiteSpace(ocrResult.Vendor))
                {
                    ocrResult.Vendor = HtmlEncoder.Default.Encode(ocrResult.Vendor.Trim());
                }

                if (!string.IsNullOrWhiteSpace(ocrResult.ReceiptNumber))
                {
                    ocrResult.ReceiptNumber = HtmlEncoder.Default.Encode(ocrResult.ReceiptNumber.Trim());
                }

                // Validate amount is reasonable (prevent overflow attacks)
                if (ocrResult.TotalAmount > 999999.99m || ocrResult.TotalAmount < 0)
                {
                    _logger.LogWarning("OCR extracted unreasonable amount: {Amount}", ocrResult.TotalAmount);
                    ocrResult.TotalAmount = 0; // Force manual entry
                }
            }

            // Step 2: Encrypt and store file
            string encryptedPath, algorithm;
            using (var stream = receiptFile.OpenReadStream())
            {
                (encryptedPath, algorithm) = await _encryptionService.EncryptFileAsync(stream, secureFileName);
            }

            _logger.LogWarning("=== ENCRYPTION COMPLETE === Path: {Path}, Algorithm: {Algorithm}, SecureFileName: {FileName}, ContentType: {ContentType}",
                encryptedPath, algorithm, secureFileName, receiptFile.ContentType);

            // Step 3: Create receipt record
            var receipt = new Receipt
            {
                // Use OCR data if available and valid, otherwise use manual input
                ReceiptNumber = !string.IsNullOrWhiteSpace(ocrResult.ReceiptNumber) ? ocrResult.ReceiptNumber : model.ReceiptNumber,
                Vendor = !string.IsNullOrWhiteSpace(ocrResult.Vendor) && ocrResult.Vendor != "Unknown" ? ocrResult.Vendor : model.Vendor,
                PurchaseDate = ocrResult.PurchaseDate != default ? ocrResult.PurchaseDate : model.PurchaseDate,
                TotalAmount = ocrResult.TotalAmount > 0 ? ocrResult.TotalAmount : model.TotalAmount,
                CardLastFour = !string.IsNullOrWhiteSpace(ocrResult.CardLastFour) ? ocrResult.CardLastFour : model.CardLastFour,
                PaymentMethod = !string.IsNullOrWhiteSpace(ocrResult.PaymentMethod) && ocrResult.PaymentMethod != PaymentMethods.Unknown
                    ? ocrResult.PaymentMethod
                    : model.PaymentMethod,
                Category = model.Category,
                Notes = model.Notes,

                // File metadata
                EncryptedFilePath = encryptedPath,
                OriginalFileName = secureFileName,
                ContentType = receiptFile.ContentType,
                FileSizeBytes = receiptFile.Length,

                // OCR metadata
                OcrRawText = ocrResult.RawText,
                OcrConfidence = ocrResult.Confidence,
                OcrProcessed = ocrResult.Success,
                OcrError = ocrResult.ErrorMessage?.Length > 500 
                    ? ocrResult.ErrorMessage.Substring(0, 500) 
                    : ocrResult.ErrorMessage,

                // Security metadata
                IsEncrypted = true,
                EncryptionAlgorithm = algorithm,

                // Audit trail (OWASP ASVS L2)
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,

                // Data retention
                ScheduledPurgeDate = CalculatePurgeDate()
            };

            _logger.LogWarning("=== RECEIPT OBJECT CREATED === EncryptedPath: {EncPath}, OriginalFileName: {OrigFile}, ContentType: {ContentType}, CreatedBy: {CreatedBy}, OcrErrorLength: {OcrErrorLen}",
                receipt.EncryptedFilePath, receipt.OriginalFileName, receipt.ContentType, receipt.CreatedBy, receipt.OcrError?.Length ?? 0);

            _logger.LogWarning("=== RECEIPT DATA === Vendor: {Vendor}, Amount: {Amount}, Date: {Date}, Category: {Category}, Payment: {Payment}",
                receipt.Vendor, receipt.TotalAmount, receipt.PurchaseDate, receipt.Category, receipt.PaymentMethod);

            // Validate model
            if (!TryValidateModel(receipt))
            {
                _logger.LogWarning("=== MODEL VALIDATION FAILED === Errors: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                PopulateReceiptFormData();
                return View("Upload", model);
            }

            // Save to database
            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();

            // Record successful upload for rate limiting
            await _rateLimitService.RecordUploadAsync(clientIp);

            stopwatch.Stop();

            _logger.LogInformation(
                "Receipt uploaded successfully: ID={ReceiptId}, Vendor={Vendor}, Amount={Amount}, OCR={OcrSuccess}, Latency={Latency}ms, User={User}, IP={IpAddress}",
                receipt.Id, receipt.Vendor, receipt.TotalAmount, ocrResult.Success, stopwatch.ElapsedMilliseconds, currentUser, GetClientIpAddress());

            _logger.LogWarning("=== RECEIPT UPLOAD SUCCESS === ID: {Id}, Vendor: {Vendor}, Amount: {Amount}", 
                receipt.Id, receipt.Vendor, receipt.TotalAmount);

            if (stopwatch.ElapsedMilliseconds > 200)
            {
                _logger.LogWarning("Upload processing exceeded p95 target: {Latency}ms", stopwatch.ElapsedMilliseconds);
            }

            SetSuccessMessage(ocrResult.Success
                ? $"Receipt uploaded successfully! OCR extracted: {receipt.Vendor} - ${receipt.TotalAmount:F2}"
                : "Receipt uploaded successfully! Please verify the details.");

            return RedirectToAction(nameof(Details), new { id = receipt.Id });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "=== RECEIPT UPLOAD FAILED === Error: {Error}, User: {User}", 
                ex.Message, currentUser);
            SetErrorMessage("An error occurred while uploading the receipt.");
            PopulateReceiptFormData();
            return View("Upload", model);
        }
    }

    /// <summary>
    /// Display receipt details
    /// Security: Authorization check, access logging, XSS protection
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUser = GetCurrentUserId();

        try
        {
            // Load receipt without access logs first (prevent N+1 query issue)
            var receipt = await _context.Receipts
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (receipt == null)
            {
                _logger.LogWarning("Receipt not found: ID={ReceiptId}, User={User}", id, currentUser);
                return NotFound();
            }

            // Load last 10 access logs in separate query (better performance)
            receipt.AccessLogs = await _context.ReceiptAccessLogs
                .Where(l => l.ReceiptId == id)
                .OrderByDescending(l => l.AccessedAt)
                .Take(10)
                .ToListAsync();

            // Log access for audit trail (OWASP ASVS L2)
            await LogAccessAsync(receipt, "View", currentUser);

            stopwatch.Stop();
            _logger.LogInformation("Receipt details viewed: ID={ReceiptId}, Latency={Latency}ms, User={User}",
                id, stopwatch.ElapsedMilliseconds, currentUser);

            return View(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading receipt details: ID={ReceiptId}, User={User}", id, currentUser);
            SetErrorMessage("An error occurred while loading receipt details.");
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Download encrypted receipt file
    /// Security: Authorization check, access logging, secure file delivery
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var currentUser = GetCurrentUserId();

        try
        {
            var receipt = await _context.Receipts.FindAsync(id);

            if (receipt == null || receipt.IsDeleted)
            {
                _logger.LogWarning("Receipt not found for download: ID={ReceiptId}, User={User}", id, currentUser);
                return NotFound();
            }

            // Decrypt file
            var decryptedStream = await _encryptionService.DecryptFileAsync(receipt.EncryptedFilePath);

            // Log access
            await LogAccessAsync(receipt, "Download", currentUser);

            _logger.LogInformation(
                "Receipt downloaded: ID={ReceiptId}, User={User}, IP={IpAddress}", 
                id, currentUser, GetClientIpAddress());

            // Return file with original name and content type
            return File(decryptedStream, receipt.ContentType, receipt.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading receipt: ID={ReceiptId}, User={User}", id, currentUser);
            SetErrorMessage("An error occurred while downloading the receipt.");
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// Soft delete receipt
    /// Security: CSRF protection, authorization check, audit logging
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = GetCurrentUserId();

        try
        {
            var receipt = await _context.Receipts.FindAsync(id);

            if (receipt == null || receipt.IsDeleted)
            {
                _logger.LogWarning("Receipt not found for deletion: ID={ReceiptId}, User={User}", id, currentUser);
                return NotFound();
            }

            // Soft delete (GDPR compliance, data retention)
            receipt.IsDeleted = true;
            receipt.DeletedAt = DateTime.UtcNow;
            receipt.DeletedBy = currentUser;

            // Update purge date (30-day grace period)
            receipt.ScheduledPurgeDate = DateTime.UtcNow.AddDays(30);

            await _context.SaveChangesAsync();

            // Log deletion
            await LogAccessAsync(receipt, "Delete", currentUser);

            _logger.LogInformation(
                "Receipt soft deleted: ID={ReceiptId}, User={User}, IP={IpAddress}", 
                id, currentUser, GetClientIpAddress());

            SetSuccessMessage("Receipt deleted successfully. It will be permanently purged after 30 days.");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting receipt: ID={ReceiptId}, User={User}", id, currentUser);
            SetErrorMessage("An error occurred while deleting the receipt.");
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// AJAX endpoint: Process OCR on uploaded file and return extracted data
    /// Security: CSRF protection, file validation, rate limiting
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)] // 30MB limit for OCR uploads
    public async Task<IActionResult> ProcessOcr(IFormFile file)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUser = GetCurrentUserId();

        try
        {
            // Security: Rate limiting check
            var clientIp = GetClientIpAddress();
            var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(clientIp);
            
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for OCR processing: IP={IpAddress}, Attempts={Attempts}/{Max}",
                    clientIp, rateLimitResult.AttemptsInWindow, rateLimitResult.MaxAllowed);
                    
                return Json(new { 
                    success = false, 
                    error = "Too many upload attempts. Please try again later." 
                });
            }

            // Validate file upload
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, error = "No file provided." });
            }

            // Security: Comprehensive file validation
            var (isValid, errorMessage, secureFileName) = FileValidationHelper.ValidateFileUpload(
                file.FileName,
                file.Length,
                file.ContentType,
                _allowedExtensions,
                _maxFileSize);

            if (!isValid)
            {
                _logger.LogWarning(
                    "OCR file validation failed: {Error}, FileName: {FileName}, User: {User}",
                    errorMessage, file.FileName, currentUser);
                return Json(new { success = false, error = errorMessage });
            }

            _logger.LogInformation("Processing OCR for file: {FileName} ({Size} bytes) by {User}",
                file.FileName, file.Length, currentUser);

            // Run OCR Processing
            OcrResult ocrResult;
            using (var stream = file.OpenReadStream())
            {
                ocrResult = await _ocrService.ExtractReceiptDataAsync(stream, file.ContentType);
            }

            stopwatch.Stop();

            if (!ocrResult.Success)
            {
                _logger.LogWarning("OCR processing failed: {Error}", ocrResult.ErrorMessage);
                return Json(new { 
                    success = false, 
                    error = ocrResult.ErrorMessage ?? "Could not extract receipt data. Please enter manually." 
                });
            }

            // Validate OCR results
            var isValidOcr = _ocrService.ValidateOcrResults(ocrResult);

            _logger.LogInformation(
                "OCR processing completed: Vendor={Vendor}, Amount={Amount}, Date={Date}, Confidence={Confidence}, Valid={Valid}, Latency={Latency}ms",
                ocrResult.Vendor, ocrResult.TotalAmount, ocrResult.PurchaseDate, ocrResult.Confidence, isValidOcr, stopwatch.ElapsedMilliseconds);

            // Return extracted data as JSON
            return Json(new
            {
                success = true,
                data = new
                {
                    vendor = ocrResult.Vendor,
                    purchaseDate = ocrResult.PurchaseDate != default ? ocrResult.PurchaseDate.ToString("yyyy-MM-dd") : null,
                    totalAmount = ocrResult.TotalAmount > 0 ? ocrResult.TotalAmount : (decimal?)null,
                    receiptNumber = ocrResult.ReceiptNumber,
                    paymentMethod = ocrResult.PaymentMethod,
                    cardLastFour = ocrResult.CardLastFour,
                    confidence = ocrResult.Confidence,
                    isValid = isValidOcr
                },
                message = isValidOcr 
                    ? $"Receipt data extracted successfully (Confidence: {ocrResult.Confidence:P0})" 
                    : "Receipt data extracted with low confidence. Please verify the details."
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing OCR (User: {User})", currentUser);
            return Json(new { 
                success = false, 
                error = "An error occurred while processing the receipt. Please try again." 
            });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Logs receipt access for audit trail (OWASP ASVS L2 requirement)
    /// </summary>
    private async Task LogAccessAsync(Receipt receipt, string action, string user)
    {
        // Create access log entry
        var log = new ReceiptAccessLog
        {
            ReceiptId = receipt.Id,
            AccessedBy = user,
            AccessedAt = DateTime.UtcNow,
            Action = action,
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        };

        _context.Add(log);

        // Update receipt access metadata
        receipt.LastAccessedAt = DateTime.UtcNow;
        receipt.LastAccessedBy = user;
        receipt.AccessCount++;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Calculates purge date based on retention policy
    /// </summary>
    private DateTime CalculatePurgeDate()
    {
        var retentionMonths = _configuration.GetValue<int>("ReceiptSettings:RetentionPeriodMonths", 84); // Default: 7 years
        return DateTime.UtcNow.AddMonths(retentionMonths);
    }

    /// <summary>
    /// Populates ViewBag with receipt form dropdown data (reduces code duplication)
    /// </summary>
    private void PopulateReceiptFormData()
    {
        ViewBag.Categories = Enum.GetValues<ExpenseCategory>();
        ViewBag.PaymentMethods = new[]
        {
            PaymentMethods.CreditCard,
            PaymentMethods.DebitCard,
            PaymentMethods.Cash,
            PaymentMethods.Check,
            PaymentMethods.BankTransfer
        };
    }

    /// <summary>
    /// Print-friendly view of receipts filtered by date range
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PrintView(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                return BadRequest("Start date and end date are required.");
            }

            if (startDate > endDate)
            {
                return BadRequest("Start date must be before or equal to end date.");
            }

            var receipts = await _context.Receipts
                .Where(r => !r.IsDeleted &&
                           r.PurchaseDate >= startDate.Value &&
                           r.PurchaseDate <= endDate.Value)
                .OrderBy(r => r.PurchaseDate)
                .ThenBy(r => r.Vendor)
                .ToListAsync();

            ViewBag.StartDate = startDate.Value;
            ViewBag.EndDate = endDate.Value;
            ViewBag.TotalAmount = receipts.Sum(r => r.TotalAmount);

            return View(receipts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating print view for receipts");
            return StatusCode(500, "An error occurred while generating the print view.");
        }
    }

    #endregion
}
