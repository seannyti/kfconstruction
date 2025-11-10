namespace KfConstructionWeb.Models;

/// <summary>
/// Result of rate limit checking
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Number of attempts made in the current window
    /// </summary>
    public int AttemptsInWindow { get; set; }

    /// <summary>
    /// Maximum allowed attempts in the window
    /// </summary>
    public int MaxAllowed { get; set; }

    /// <summary>
    /// Duration of the rate limit window
    /// </summary>
    public TimeSpan WindowDuration { get; set; }

    /// <summary>
    /// When the next attempt will be allowed (if currently blocked)
    /// </summary>
    public DateTime? NextAllowedTime { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result of duplicate content checking
/// </summary>
public class DuplicateCheckResult
{
    /// <summary>
    /// Whether the content is a duplicate
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// Similarity score (0.0 to 1.0)
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Hash of the content for comparison
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// When the original similar content was submitted
    /// </summary>
    public DateTime? OriginalSubmissionDate { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result of spam detection analysis
/// </summary>
public class SpamDetectionResult
{
    /// <summary>
    /// Whether the content is classified as spam
    /// </summary>
    public bool IsSpam { get; set; }

    /// <summary>
    /// Spam probability score (0.0 to 1.0)
    /// </summary>
    public double SpamScore { get; set; }

    /// <summary>
    /// Risk level classification
    /// </summary>
    public SpamRiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Reasons for the classification
    /// </summary>
    public List<string> Reasons { get; set; } = new();

    /// <summary>
    /// Detailed analysis results
    /// </summary>
    public Dictionary<string, object> AnalysisDetails { get; set; } = new();

    /// <summary>
    /// Confidence level in the analysis (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; } = 1.0;
}

/// <summary>
/// Risk levels for spam detection
/// </summary>
public enum SpamRiskLevel
{
    /// <summary>
    /// Very low risk - content appears highly legitimate
    /// </summary>
    VeryLow = 0,

    /// <summary>
    /// Low risk - minor suspicious indicators
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium risk - multiple suspicious indicators
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High risk - strong spam indicators
    /// </summary>
    High = 3,

    /// <summary>
    /// Unknown risk - analysis failed or incomplete
    /// </summary>
    Unknown = 4
}