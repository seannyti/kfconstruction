using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KfConstructionWeb.Models.Validation;

namespace KfConstructionWeb.Models;

/// <summary>
/// Represents a client testimonial or review
/// </summary>
public class Testimonial
{
    /// <summary>
    /// Unique identifier for the testimonial
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the person providing the testimonial
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    [ProfessionalName]
    [Display(Name = "Client Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the reviewer (not displayed publicly)
    /// </summary>
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(256)]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    /// <summary>
    /// Company name if applicable
    /// </summary>
    [StringLength(200)]
    [Display(Name = "Company Name")]
    public string? Company { get; set; }

    /// <summary>
    /// Job title or position
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// The testimonial content
    /// </summary>
    [Required(ErrorMessage = "Testimonial content is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Testimonial must be between 10 and 2000 characters")]
    [ContentFilter]
    [MeaningfulContent(8)]
    [Display(Name = "Testimonial")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Rating given by the client (1-5 stars)
    /// </summary>
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
    [Display(Name = "Rating (1-5 stars)")]
    public int Rating { get; set; } = 5;

    /// <summary>
    /// Associated project ID (optional)
    /// </summary>
    [Display(Name = "Related Project")]
    public int? ProjectId { get; set; }

    /// <summary>
    /// Associated client ID (if logged in client)
    /// </summary>
    public int? ClientId { get; set; }

    /// <summary>
    /// Current status of the testimonial
    /// </summary>
    [Required]
    public TestimonialStatus Status { get; set; } = TestimonialStatus.Pending;

    /// <summary>
    /// Whether this testimonial is featured prominently
    /// </summary>
    [Display(Name = "Featured Testimonial")]
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Display order for testimonials (lower numbers first)
    /// </summary>
    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Location/city of the client
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Location")]
    public string? Location { get; set; }

    /// <summary>
    /// URL to client's profile photo (optional)
    /// </summary>
    [StringLength(500)]
    [Url(ErrorMessage = "Please enter a valid URL")]
    [Display(Name = "Photo URL")]
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Type of work/service reviewed
    /// </summary>
    [Display(Name = "Service Type")]
    public ServiceType? ServiceType { get; set; }

    /// <summary>
    /// When this testimonial was submitted
    /// </summary>
    [Display(Name = "Submitted Date")]
    [DataType(DataType.DateTime)]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this testimonial was approved/published
    /// </summary>
    [Display(Name = "Published Date")]
    [DataType(DataType.DateTime)]
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// When this testimonial record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this testimonial record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Admin notes (not visible to public)
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Admin Notes")]
    public string? AdminNotes { get; set; }

    /// <summary>
    /// IP address of submitter (for spam prevention)
    /// </summary>
    [StringLength(45)]
    public string? IPAddress { get; set; }

    /// <summary>
    /// User agent of submitter (for spam prevention)
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Navigation property to the associated client
    /// </summary>
    [ForeignKey(nameof(ClientId))]
    public virtual Client? Client { get; set; }

    /// <summary>
    /// Display name for the testimonial author
    /// </summary>
    [NotMapped]
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(Company) && !string.IsNullOrEmpty(JobTitle))
                return $"{Name}, {JobTitle} at {Company}";
            if (!string.IsNullOrEmpty(Company))
                return $"{Name}, {Company}";
            if (!string.IsNullOrEmpty(JobTitle))
                return $"{Name}, {JobTitle}";
            return Name;
        }
    }

    /// <summary>
    /// Gets star rating as HTML for display
    /// </summary>
    [NotMapped]
    public string StarRatingHtml
    {
        get
        {
            var stars = "";
            for (int i = 1; i <= 5; i++)
            {
                if (i <= Rating)
                    stars += "★";
                else
                    stars += "☆";
            }
            return stars;
        }
    }
}

/// <summary>
/// Status options for testimonials
/// </summary>
public enum TestimonialStatus
{
    /// <summary>
    /// Submitted but awaiting review
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Approved and published
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Rejected or hidden
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Flagged for review
    /// </summary>
    Flagged = 4,

    /// <summary>
    /// Archived/no longer displayed
    /// </summary>
    Archived = 5
}