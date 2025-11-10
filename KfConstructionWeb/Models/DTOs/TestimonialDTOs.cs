using System.ComponentModel.DataAnnotations;
using KfConstructionWeb.Models.Validation;

namespace KfConstructionWeb.Models.DTOs;

/// <summary>
/// Request model for testimonial submission
/// </summary>
public class TestimonialSubmissionRequest
{
    /// <summary>
    /// Author's professional name
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [ProfessionalName(ErrorMessage = "Please provide a valid professional name")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Author's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [StringLength(254, ErrorMessage = "Email address is too long")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Author's company or organization
    /// </summary>
    [StringLength(200, ErrorMessage = "Company name is too long")]
    public string? Company { get; set; }

    /// <summary>
    /// Author's job title or position
    /// </summary>
    [StringLength(100, ErrorMessage = "Position is too long")]
    public string? Position { get; set; }

    /// <summary>
    /// Project or service type related to the testimonial
    /// </summary>
    [StringLength(100, ErrorMessage = "Project type is too long")]
    public string? ProjectType { get; set; }

    /// <summary>
    /// The testimonial content
    /// </summary>
    [Required(ErrorMessage = "Testimonial content is required")]
    [ContentFilter(ErrorMessage = "Content contains inappropriate language or patterns")]
    [MeaningfulContent(ErrorMessage = "Please provide meaningful, descriptive content")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Testimonial must be between 10 and 2000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Rating given by the client (1-5 stars)
    /// </summary>
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
    public int Rating { get; set; } = 5;

    /// <summary>
    /// Whether to include the client's company in the display
    /// </summary>
    public bool ShowCompany { get; set; } = true;

    /// <summary>
    /// Whether to include the client's position in the display
    /// </summary>
    public bool ShowPosition { get; set; } = true;

    /// <summary>
    /// Honeypot field for spam protection (should be empty)
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Client's IP address (populated by controller)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Browser fingerprint data (populated by controller)
    /// </summary>
    public string? BrowserFingerprint { get; set; }

    /// <summary>
    /// Submission timestamp (populated by controller)
    /// </summary>
    public DateTime SubmissionTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of testimonial submission processing
/// </summary>
public class TestimonialSubmissionResult
{
    /// <summary>
    /// Whether the submission was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message for display to user
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Internal error details (for logging)
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Validation errors by field
    /// </summary>
    public Dictionary<string, List<string>> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Security analysis results
    /// </summary>
    public SecurityAnalysisResult? SecurityAnalysis { get; set; }

    /// <summary>
    /// ID of the created testimonial (if successful)
    /// </summary>
    public int? TestimonialId { get; set; }

    /// <summary>
    /// Whether the testimonial requires manual approval
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Security analysis results for submission
/// </summary>
public class SecurityAnalysisResult
{
    /// <summary>
    /// Overall security score (0.0 to 1.0, lower is safer)
    /// </summary>
    public double SecurityScore { get; set; }

    /// <summary>
    /// Whether spam was detected
    /// </summary>
    public bool SpamDetected { get; set; }

    /// <summary>
    /// Rate limit status
    /// </summary>
    public bool RateLimited { get; set; }

    /// <summary>
    /// Duplicate content detected
    /// </summary>
    public bool DuplicateContent { get; set; }

    /// <summary>
    /// Security warnings and flags
    /// </summary>
    public List<string> SecurityFlags { get; set; } = new();

    /// <summary>
    /// Risk level assessment
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    /// Detailed analysis data
    /// </summary>
    public Dictionary<string, object> AnalysisData { get; set; } = new();
}