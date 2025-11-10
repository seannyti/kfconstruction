using KfConstructionWeb.Models;

namespace KfConstructionWeb.Services.Interfaces;

public interface ITestimonialRateLimitService
{
    /// <summary>
    /// Checks if an IP address can submit a testimonial
    /// </summary>
    Task<RateLimitResult> CheckRateLimitAsync(string ipAddress);

    /// <summary>
    /// Checks if content is duplicate
    /// </summary>
    Task<DuplicateCheckResult> CheckDuplicateContentAsync(string content, string? email = null);

    /// <summary>
    /// Records a submission for rate limiting
    /// </summary>
    Task RecordSubmissionAsync(string ipAddress);

    /// <summary>
    /// Records content to prevent duplicates
    /// </summary>
    Task RecordContentAsync(string content, string? email = null);

    /// <summary>
    /// Performs advanced spam detection
    /// </summary>
    Task<SpamDetectionResult> PerformAdvancedSpamDetectionAsync(string content, string? email = null, string? ipAddress = null);

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    Task<bool> CanSubmitAsync(string ipAddress);

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    Task<bool> IsDuplicateContentAsync(string content, string? email = null);
}
