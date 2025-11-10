using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KfConstructionWeb.Models;

/// <summary>
/// Represents a scanned receipt with OCR data and audit trail
/// OWASP ASVS L2 compliant with encryption and audit logging
/// </summary>
public class Receipt
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Receipt number extracted from OCR or manually entered
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Receipt Number")]
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Vendor/merchant name
    /// </summary>
    [Required]
    [StringLength(200)]
    [Display(Name = "Vendor")]
    public string Vendor { get; set; } = string.Empty;

    /// <summary>
    /// Date of purchase
    /// </summary>
    [Required]
    [Display(Name = "Purchase Date")]
    [DataType(DataType.Date)]
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Total amount from receipt
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999.99)]
    [Display(Name = "Total Amount")]
    [DataType(DataType.Currency)]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Last 4 digits of payment card (PII minimization)
    /// </summary>
    [StringLength(4, MinimumLength = 4)]
    [Display(Name = "Card Last 4")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Must be exactly 4 digits")]
    public string? CardLastFour { get; set; }

    /// <summary>
    /// Payment method type
    /// </summary>
    [Required]
    [StringLength(50)]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Unknown";

    /// <summary>
    /// Expense category for accounting
    /// </summary>
    [Required]
    [Display(Name = "Category")]
    public ExpenseCategory Category { get; set; }

    /// <summary>
    /// Additional notes or description
    /// </summary>
    [StringLength(1000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Encrypted file path to receipt image/PDF
    /// </summary>
    [Required]
    [StringLength(500)]
    public string EncryptedFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Original filename (sanitized)
    /// </summary>
    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of uploaded file
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Required]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Raw OCR text extracted from receipt
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? OcrRawText { get; set; }

    /// <summary>
    /// OCR confidence score (0-1)
    /// </summary>
    [Range(0, 1)]
    public double? OcrConfidence { get; set; }

    /// <summary>
    /// Whether OCR was successful
    /// </summary>
    public bool OcrProcessed { get; set; }

    /// <summary>
    /// OCR processing error message
    /// </summary>
    [StringLength(500)]
    public string? OcrError { get; set; }

    /// <summary>
    /// Whether file is encrypted at rest
    /// </summary>
    [Required]
    public bool IsEncrypted { get; set; } = true;

    /// <summary>
    /// Encryption algorithm used
    /// </summary>
    [StringLength(50)]
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Soft delete flag for data retention compliance
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Date when record was soft deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who deleted the record
    /// </summary>
    [StringLength(256)]
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Scheduled purge date based on retention policy
    /// </summary>
    public DateTime? ScheduledPurgeDate { get; set; }

    // Audit fields (OWASP ASVS L2 requirement)

    /// <summary>
    /// When record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the record
    /// </summary>
    [Required]
    [StringLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When record was last modified
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the record
    /// </summary>
    [StringLength(256)]
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Last time file was accessed (for audit trail)
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// User who last accessed the file
    /// </summary>
    [StringLength(256)]
    public string? LastAccessedBy { get; set; }

    /// <summary>
    /// Number of times file has been accessed
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// Navigation property for access audit logs
    /// </summary>
    public virtual ICollection<ReceiptAccessLog> AccessLogs { get; set; } = new List<ReceiptAccessLog>();
}

/// <summary>
/// Expense categories for accounting and reporting
/// </summary>
public enum ExpenseCategory
{
    Materials = 1,
    Equipment = 2,
    Tools = 3,
    Labor = 4,
    Subcontractor = 5,
    Fuel = 6,
    Vehicle = 7,
    Office = 8,
    Marketing = 9,
    Insurance = 10,
    Permits = 11,
    Utilities = 12,
    Maintenance = 13,
    Travel = 14,
    Meals = 15,
    Other = 16
}

/// <summary>
/// Audit log for receipt file access (OWASP ASVS L2 compliance)
/// </summary>
public class ReceiptAccessLog
{
    public int Id { get; set; }

    [Required]
    public int ReceiptId { get; set; }

    [Required]
    [StringLength(256)]
    public string AccessedBy { get; set; } = string.Empty;

    [Required]
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // View, Download, Edit, Delete

    [StringLength(100)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public virtual Receipt Receipt { get; set; } = null!;
}

/// <summary>
/// Payment method types
/// </summary>
public static class PaymentMethods
{
    public const string CreditCard = "Credit Card";
    public const string DebitCard = "Debit Card";
    public const string Cash = "Cash";
    public const string Check = "Check";
    public const string BankTransfer = "Bank Transfer";
    public const string Unknown = "Unknown";
}
