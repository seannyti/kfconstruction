namespace KfConstructionWeb.Models.Configuration;

/// <summary>
/// Configuration settings for testimonial functionality
/// </summary>
public class TestimonialSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "TestimonialSettings";

    /// <summary>
    /// Maximum number of testimonials per IP per day
    /// </summary>
    public int MaxSubmissionsPerDay { get; set; } = 3;

    /// <summary>
    /// Rate limiting window in hours
    /// </summary>
    public int RateLimitWindowHours { get; set; } = 24;

    /// <summary>
    /// Duplicate content detection window in days
    /// </summary>
    public int DuplicateDetectionWindowDays { get; set; } = 30;

    /// <summary>
    /// Minimum form fill time in seconds (spam protection)
    /// </summary>
    public int MinimumFormFillTimeSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum form fill time in minutes (session timeout)
    /// </summary>
    public int MaximumFormFillTimeMinutes { get; set; } = 60;

    /// <summary>
    /// Enable automated moderation features
    /// </summary>
    public bool EnableAutomatedModeration { get; set; } = true;

    /// <summary>
    /// Automatically approve testimonials from verified clients
    /// </summary>
    public bool AutoApproveVerifiedClients { get; set; } = false;

    /// <summary>
    /// Send email notifications to admins for new testimonials
    /// </summary>
    public bool SendAdminNotifications { get; set; } = true;

    /// <summary>
    /// Admin email addresses for testimonial notifications
    /// </summary>
    public List<string> AdminNotificationEmails { get; set; } = new();

    /// <summary>
    /// Number of featured testimonials to display on homepage
    /// </summary>
    public int FeaturedTestimonialsCount { get; set; } = 3;

    /// <summary>
    /// Enable testimonial analytics tracking
    /// </summary>
    public bool EnableAnalytics { get; set; } = true;
}