namespace KfConstructionWeb.Models.Configuration;

/// <summary>
/// Security configuration for testimonial system
/// </summary>
public class SecurityConfiguration
{
    /// <summary>
    /// Maximum number of testimonial submissions per day per IP address
    /// </summary>
    public int MaxSubmissionsPerDay { get; set; } = 3;

    /// <summary>
    /// Rate limit window in hours
    /// </summary>
    public int RateLimitWindowHours { get; set; } = 24;

    /// <summary>
    /// Number of days to check for duplicate content
    /// </summary>
    public int DuplicateDetectionDays { get; set; } = 30;

    /// <summary>
    /// Enable advanced spam detection
    /// </summary>
    public bool EnableAdvancedSpamDetection { get; set; } = true;

    /// <summary>
    /// Spam score threshold for blocking (0.0 to 1.0)
    /// </summary>
    public double SpamScoreThreshold { get; set; } = 0.8;

    /// <summary>
    /// Enable browser fingerprinting for bot detection
    /// </summary>
    public bool EnableBrowserFingerprinting { get; set; } = true;

    /// <summary>
    /// Minimum content length required
    /// </summary>
    public int MinContentLength { get; set; } = 10;

    /// <summary>
    /// Maximum content length allowed
    /// </summary>
    public int MaxContentLength { get; set; } = 2000;

    /// <summary>
    /// List of blocked domains for email addresses
    /// </summary>
    public List<string> BlockedEmailDomains { get; set; } = new()
    {
        "tempmail.com",
        "10minutemail.com",
        "guerrillamail.com",
        "mailinator.com"
    };

    /// <summary>
    /// List of blocked IP addresses or CIDR ranges
    /// </summary>
    public List<string> BlockedIpAddresses { get; set; } = new();

    /// <summary>
    /// Enable content quality analysis
    /// </summary>
    public bool EnableContentQualityAnalysis { get; set; } = true;

    /// <summary>
    /// Enable honeypot field validation
    /// </summary>
    public bool EnableHoneypotValidation { get; set; } = true;

    /// <summary>
    /// Name of the honeypot field
    /// </summary>
    public string HoneypotFieldName { get; set; } = "website";
}