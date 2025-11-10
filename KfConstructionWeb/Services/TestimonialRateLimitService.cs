using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.Configuration;
using KfConstructionWeb.Models.Exceptions;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace KfConstructionWeb.Services;

/// <summary>
/// Advanced spam detection and rate limiting service for testimonials
/// </summary>
public class TestimonialRateLimitService : ITestimonialRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TestimonialRateLimitService> _logger;
    private readonly SecurityConfiguration _securityConfig;
    private readonly int _maxSubmissionsPerDay;
    private readonly TimeSpan _rateLimitWindow;
    private readonly TimeSpan _duplicateWindow;
    private readonly ConcurrentDictionary<string, BrowserFingerprint> _browserFingerprints;

    public TestimonialRateLimitService(
        IMemoryCache cache,
        ILogger<TestimonialRateLimitService> logger,
        IOptions<SecurityConfiguration> securityConfig)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityConfig = securityConfig?.Value ?? throw new ArgumentNullException(nameof(securityConfig));
        
        _maxSubmissionsPerDay = _securityConfig.MaxSubmissionsPerDay;
        _rateLimitWindow = TimeSpan.FromHours(_securityConfig.RateLimitWindowHours);
        _duplicateWindow = TimeSpan.FromDays(_securityConfig.DuplicateDetectionDays);
        _browserFingerprints = new ConcurrentDictionary<string, BrowserFingerprint>();
    }

    /// <inheritdoc />
    public async Task<bool> CanSubmitAsync(string ipAddress)
    {
        var result = await CheckRateLimitAsync(ipAddress);
        return result.IsAllowed;
    }

    /// <inheritdoc />
    public async Task<bool> IsDuplicateContentAsync(string content, string? email = null)
    {
        var result = await CheckDuplicateContentAsync(content, email);
        return result.IsDuplicate;
    }

    /// <inheritdoc />
    public Task<RateLimitResult> CheckRateLimitAsync(string ipAddress)
    {
        var result = new RateLimitResult
        {
            MaxAllowed = _maxSubmissionsPerDay,
            WindowDuration = _rateLimitWindow
        };

        if (string.IsNullOrEmpty(ipAddress))
        {
            result.IsAllowed = false;
            result.Message = "Invalid IP address";
            return Task.FromResult(result);
        }

        var cacheKey = $"testimonial_rate_limit_{ipAddress}";
        
        if (_cache.TryGetValue(cacheKey, out List<DateTime>? submissions))
        {
            if (submissions != null)
            {
                // Remove old submissions outside the window
                var cutoff = DateTime.UtcNow.Subtract(_rateLimitWindow);
                submissions.RemoveAll(s => s < cutoff);
                result.AttemptsInWindow = submissions.Count;

                if (submissions.Count >= _maxSubmissionsPerDay)
                {
                    result.IsAllowed = false;
                    result.NextAllowedTime = submissions.Min().Add(_rateLimitWindow);
                    result.Message = $"Rate limit exceeded. Try again after {result.NextAllowedTime:yyyy-MM-dd HH:mm:ss} UTC.";
                    return Task.FromResult(result);
                }
            }
        }

        result.IsAllowed = true;
        result.Message = "Submission allowed";
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<DuplicateCheckResult> CheckDuplicateContentAsync(string content, string? email = null)
    {
        var result = new DuplicateCheckResult();
        
        if (string.IsNullOrWhiteSpace(content))
        {
            result.IsDuplicate = false;
            result.Message = "No content to check";
            return Task.FromResult(result);
        }

        // Create a normalized hash of the content
        var normalizedContent = NormalizeContent(content);
        var contentHash = ComputeContentHash(normalizedContent, email);
        result.ContentHash = contentHash;

        var cacheKey = $"testimonial_content_{contentHash}";
        
        if (_cache.TryGetValue(cacheKey, out DateTime lastSubmission))
        {
            var timeSinceLastSubmission = DateTime.UtcNow - lastSubmission;
            result.OriginalSubmissionDate = lastSubmission;
            
            if (timeSinceLastSubmission < _duplicateWindow)
            {
                result.IsDuplicate = true;
                result.SimilarityScore = 1.0; // Exact match
                result.Message = $"Duplicate content detected. Similar testimonial was submitted on {lastSubmission:yyyy-MM-dd}";
                return Task.FromResult(result);
            }
        }

        result.IsDuplicate = false;
        result.Message = "Content is unique";
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task RecordSubmissionAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return Task.CompletedTask;
        }

        var cacheKey = $"testimonial_rate_limit_{ipAddress}";
        var submissions = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = _rateLimitWindow;
            return new List<DateTime>();
        }) ?? new List<DateTime>();

        submissions.Add(DateTime.UtcNow);
        _cache.Set(cacheKey, submissions, _rateLimitWindow);

        _logger.LogInformation("Recorded testimonial submission for IP {IpAddress}", ipAddress);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordContentAsync(string content, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.CompletedTask;
        }

        var normalizedContent = NormalizeContent(content);
        var contentHash = ComputeContentHash(normalizedContent, email);
        var cacheKey = $"testimonial_content_{contentHash}";

        _cache.Set(cacheKey, DateTime.UtcNow, _duplicateWindow);

        _logger.LogInformation("Recorded content hash {ContentHash} for duplicate prevention", contentHash);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<SpamDetectionResult> PerformAdvancedSpamDetectionAsync(string content, string? email = null, string? ipAddress = null)
    {
        var result = new SpamDetectionResult();
        
        try
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(content))
            {
                result.IsSpam = true;
                result.SpamScore = 1.0;
                result.Reasons.Add("Empty or whitespace-only content");
                result.RiskLevel = SpamRiskLevel.High;
                return result;
            }

            var tasks = new List<Task<double>>
            {
                Task.Run(() => CalculateContentQualityScore(content)),
                Task.Run(() => CalculateLanguageComplexity(content)),
                Task.Run(() => CalculateGenericContentScore(content))
            };

            if (!string.IsNullOrEmpty(email))
            {
                tasks.Add(Task.Run(() => AnalyzeEmailPattern(email)));
            }

            if (!string.IsNullOrEmpty(ipAddress))
            {
                tasks.Add(Task.Run(() => AnalyzeBrowserFingerprint(ipAddress)));
            }

            var scores = await Task.WhenAll(tasks);
            
            // Weighted spam score calculation
            var contentQuality = scores[0];
            var languageComplexity = scores[1];
            var genericScore = scores[2];
            var emailScore = scores.Length > 3 ? scores[3] : 0.0;
            var browserScore = scores.Length > 4 ? scores[4] : 0.0;

            result.SpamScore = (contentQuality * 0.4) + (languageComplexity * 0.2) + 
                              (genericScore * 0.2) + (emailScore * 0.1) + (browserScore * 0.1);

            // Determine risk level and spam status
            if (result.SpamScore >= 0.8)
            {
                result.RiskLevel = SpamRiskLevel.High;
                result.IsSpam = true;
                result.Reasons.Add("High spam probability detected");
            }
            else if (result.SpamScore >= 0.6)
            {
                result.RiskLevel = SpamRiskLevel.Medium;
                result.IsSpam = false;
                result.Reasons.Add("Medium spam risk - manual review recommended");
            }
            else if (result.SpamScore >= 0.3)
            {
                result.RiskLevel = SpamRiskLevel.Low;
                result.IsSpam = false;
                result.Reasons.Add("Low spam risk");
            }
            else
            {
                result.RiskLevel = SpamRiskLevel.VeryLow;
                result.IsSpam = false;
                result.Reasons.Add("Content appears legitimate");
            }

            result.AnalysisDetails = new Dictionary<string, object>
            {
                { "ContentQuality", contentQuality },
                { "LanguageComplexity", languageComplexity },
                { "GenericContentScore", genericScore },
                { "EmailPatternScore", emailScore },
                { "BrowserFingerprintScore", browserScore },
                { "ContentLength", content.Length },
                { "WordCount", content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length }
            };

            _logger.LogInformation("Spam detection completed. Score: {SpamScore:F3}, Risk: {RiskLevel}", 
                result.SpamScore, result.RiskLevel);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during spam detection analysis");
            result.IsSpam = false; // Fail open
            result.SpamScore = 0.0;
            result.RiskLevel = SpamRiskLevel.Unknown;
            result.Reasons.Add("Analysis failed - allowing submission");
            return result;
        }
    }

    /// <summary>
    /// Normalizes content for duplicate detection by removing punctuation and extra spaces
    /// </summary>
    private static string NormalizeContent(string content)
    {
        // Remove punctuation, convert to lowercase, and normalize whitespace
        var normalized = Regex.Replace(content.ToLowerInvariant(), @"[^\w\s]", "");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    /// <summary>
    /// Computes a hash for content that includes optional email for better duplicate detection
    /// </summary>
    private static string ComputeContentHash(string normalizedContent, string? email = null)
    {
        var input = normalizedContent + (email ?? "");
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash)[..16]; // Take first 16 characters for cache key
    }

    /// <summary>
    /// Calculates content quality score based on various factors
    /// </summary>
    private static double CalculateContentQualityScore(string content)
    {
        double score = 0.0;
        
        // Length analysis
        if (content.Length < 10) score += 0.5; // Too short
        else if (content.Length > 2000) score += 0.3; // Suspiciously long
        
        // Character repetition analysis
        var charGroups = content.GroupBy(c => c).Where(g => g.Count() > content.Length * 0.3);
        if (charGroups.Any()) score += 0.4;
        
        // All caps analysis
        var upperCaseRatio = content.Count(char.IsUpper) / (double)content.Length;
        if (upperCaseRatio > 0.7) score += 0.3;
        
        // URL/Link detection
        if (Regex.IsMatch(content, @"https?://|www\.|\.com|\.net|\.org", RegexOptions.IgnoreCase))
            score += 0.6;
        
        // Excessive punctuation
        var punctuationRatio = content.Count(char.IsPunctuation) / (double)content.Length;
        if (punctuationRatio > 0.2) score += 0.3;
        
        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Analyzes email patterns for suspicious characteristics
    /// </summary>
    private static double AnalyzeEmailPattern(string email)
    {
        double score = 0.0;
        
        // Disposable email domains (simplified check)
        var disposableDomains = new[] { "tempmail", "10minutemail", "guerrillamail", "mailinator" };
        if (disposableDomains.Any(domain => email.Contains(domain, StringComparison.OrdinalIgnoreCase)))
            score += 0.8;
        
        // Suspicious patterns
        if (Regex.IsMatch(email, @"\d{5,}")) score += 0.4; // Too many consecutive numbers
        if (email.Count(c => c == '.') > 3) score += 0.3; // Too many dots
        if (!email.Contains('@') || email.Count(c => c == '@') != 1) score += 1.0; // Invalid format
        
        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Calculates a score for generic/template-like content
    /// </summary>
    private static double CalculateGenericContentScore(string content)
    {
        double score = 0.0;
        var lowerContent = content.ToLowerInvariant();
        
        // Generic phrases that might indicate template usage
        var genericPhrases = new[]
        {
            "lorem ipsum", "sample text", "placeholder", "test test",
            "asdasd", "qwerty", "example content", "default text"
        };
        
        foreach (var phrase in genericPhrases)
        {
            if (lowerContent.Contains(phrase))
                score += 0.7;
        }
        
        // Repetitive words
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordCounts = words.GroupBy(w => w.ToLowerInvariant())
                             .Where(g => g.Count() > Math.Max(2, words.Length * 0.3));
        
        if (wordCounts.Any())
            score += 0.5;
        
        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Calculates language complexity to detect very simple or bot-generated content
    /// </summary>
    private static double CalculateLanguageComplexity(string content)
    {
        double score = 0.0;
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0) return 1.0;
        
        // Average word length
        var avgWordLength = words.Average(w => w.Length);
        if (avgWordLength < 3) score += 0.4; // Very short words
        if (avgWordLength > 12) score += 0.3; // Suspiciously long words
        
        // Sentence structure (very basic)
        var sentences = content.Split('.', '!', '?');
        if (sentences.Length < 2 && content.Length > 50) score += 0.3;
        
        // Character diversity
        var uniqueChars = content.ToLowerInvariant().Distinct().Count();
        var diversityRatio = uniqueChars / (double)Math.Min(26, content.Length);
        if (diversityRatio < 0.3) score += 0.4;
        
        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Analyzes browser fingerprint patterns for bot detection
    /// </summary>
    private double AnalyzeBrowserFingerprint(string ipAddress)
    {
        try
        {
            var cacheKey = $"browser_fingerprint_{ipAddress}";
            
            if (_cache.TryGetValue(cacheKey, out BrowserFingerprint? fingerprint) && fingerprint != null)
            {
                // Analyze submission frequency
                var recentSubmissions = fingerprint.SubmissionTimes
                    .Where(t => t > DateTime.UtcNow.AddHours(-1))
                    .Count();
                
                if (recentSubmissions > 5) return 0.8; // Too many recent submissions
                if (recentSubmissions > 2) return 0.4; // Moderate frequency
                
                // Analyze time patterns
                var intervals = fingerprint.SubmissionTimes
                    .OrderBy(t => t)
                    .Zip(fingerprint.SubmissionTimes.Skip(1), (a, b) => (b - a).TotalSeconds)
                    .ToList();
                
                if (intervals.Any() && intervals.All(i => Math.Abs(i - intervals.First()) < 5))
                    return 0.9; // Suspiciously regular intervals
            }
            else
            {
                // First submission from this IP
                fingerprint = new BrowserFingerprint
                {
                    IpAddress = ipAddress,
                    FirstSeen = DateTime.UtcNow,
                    SubmissionTimes = new List<DateTime>()
                };
                
                _cache.Set(cacheKey, fingerprint, TimeSpan.FromDays(1));
            }
            
            fingerprint.SubmissionTimes.Add(DateTime.UtcNow);
            fingerprint.LastSeen = DateTime.UtcNow;
            
            return 0.0; // Normal behavior
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing browser fingerprint");
            return 0.0; // Fail open
        }
    }
}

/// <summary>
/// Browser fingerprint for tracking submission patterns
/// </summary>
public class BrowserFingerprint
{
    public string IpAddress { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public List<DateTime> SubmissionTimes { get; set; } = new();
    public string UserAgent { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}