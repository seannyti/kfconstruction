using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models.Exceptions;

/// <summary>
/// Base exception for all testimonial-related operations
/// </summary>
public abstract class TestimonialException : Exception
{
    protected TestimonialException(string message) : base(message) { }
    protected TestimonialException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when testimonial validation fails
/// </summary>
public class TestimonialValidationException : TestimonialException
{
    public List<ValidationResult> ValidationErrors { get; }

    public TestimonialValidationException(string message, List<ValidationResult> validationErrors) 
        : base(message)
    {
        ValidationErrors = validationErrors ?? new List<ValidationResult>();
    }

    public TestimonialValidationException(List<ValidationResult> validationErrors) 
        : base("Testimonial validation failed.")
    {
        ValidationErrors = validationErrors ?? new List<ValidationResult>();
    }
}

/// <summary>
/// Exception thrown when rate limiting is exceeded
/// </summary>
public class TestimonialRateLimitException : TestimonialException
{
    public string IPAddress { get; }
    public int AttemptsInWindow { get; }
    public TimeSpan WindowDuration { get; }
    public DateTime NextAllowedSubmission { get; }

    public TestimonialRateLimitException(
        string ipAddress, 
        int attemptsInWindow, 
        TimeSpan windowDuration,
        DateTime nextAllowedSubmission) 
        : base($"Rate limit exceeded for IP {ipAddress}. {attemptsInWindow} attempts in {windowDuration}. Next submission allowed at {nextAllowedSubmission:yyyy-MM-dd HH:mm:ss} UTC.")
    {
        IPAddress = ipAddress;
        AttemptsInWindow = attemptsInWindow;
        WindowDuration = windowDuration;
        NextAllowedSubmission = nextAllowedSubmission;
    }
}

/// <summary>
/// Exception thrown when duplicate content is detected
/// </summary>
public class TestimonialDuplicateContentException : TestimonialException
{
    public string ContentHash { get; }
    public DateTime OriginalSubmissionDate { get; }

    public TestimonialDuplicateContentException(string contentHash, DateTime originalSubmissionDate)
        : base("Duplicate content detected. Similar testimonial was previously submitted.")
    {
        ContentHash = contentHash;
        OriginalSubmissionDate = originalSubmissionDate;
    }
}

/// <summary>
/// Exception thrown when spam is detected
/// </summary>
public class TestimonialSpamDetectedException : TestimonialException
{
    public SpamDetectionReason Reason { get; }
    public Dictionary<string, object> DetectionMetadata { get; }

    public TestimonialSpamDetectedException(SpamDetectionReason reason, Dictionary<string, object>? metadata = null)
        : base($"Spam detected: {reason}")
    {
        Reason = reason;
        DetectionMetadata = metadata ?? new Dictionary<string, object>();
    }
}

/// <summary>
/// Reasons for spam detection
/// </summary>
public enum SpamDetectionReason
{
    HoneypotTriggered,
    SubmissionTooFast,
    SuspiciousContent,
    ProhibitedWords,
    ContactInformationDetected,
    ExcessiveRepetition,
    GenericContent,
    SuspiciousIPPattern
}