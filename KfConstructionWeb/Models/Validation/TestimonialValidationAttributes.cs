using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace KfConstructionWeb.Models.Validation;

/// <summary>
/// Custom validation attribute to prevent profanity and inappropriate content in testimonials
/// </summary>
public class ContentFilterAttribute : ValidationAttribute
{
    private static readonly string[] ProhibitedWords = {
        // Profanity and inappropriate content
        "spam", "fake", "scam", "terrible", "awful", "worst", "hate", "horrible", "disgusting",
        // Promotional/commercial content
        "discount", "coupon", "promotion", "sale", "buy now", "click here", "limited time",
        // Competitive mentions
        "competitor", "alternative", "better than", "cheaper than", "instead of"
    };

    private static readonly Regex[] SuspiciousPatterns = {
        new Regex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.IgnoreCase), // Phone numbers
        new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.IgnoreCase), // Email addresses
        new Regex(@"https?://[^\s]+", RegexOptions.IgnoreCase), // URLs
        new Regex(@"\$\d+(?:\.\d{2})?", RegexOptions.IgnoreCase), // Price mentions
        new Regex(@"\b(call|contact|visit|email|text)\s+(me|us|them)\b", RegexOptions.IgnoreCase), // Contact solicitation
        new Regex(@"\b(website|site|facebook|instagram|twitter|linkedin)\b", RegexOptions.IgnoreCase), // Social media references
        new Regex(@"\b\d+%\s*(off|discount|savings?)\b", RegexOptions.IgnoreCase), // Discount patterns
        new Regex(@"\b(free|bonus|gift|prize|winner?)\b", RegexOptions.IgnoreCase) // Promotional language
    };

    private static readonly string[] RequiredWords = {
        "work", "project", "construction", "build", "quality", "professional", "team", "service"
    };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string content || string.IsNullOrWhiteSpace(content))
        {
            return ValidationResult.Success;
        }

        var lowerContent = content.ToLowerInvariant();
        var errors = new List<string>();

        // Check for prohibited words
        foreach (var word in ProhibitedWords)
        {
            if (lowerContent.Contains(word.ToLowerInvariant()))
            {
                errors.Add($"Content contains inappropriate language: '{word}'");
                break; // Don't reveal all prohibited words
            }
        }

        // Check for suspicious patterns
        foreach (var pattern in SuspiciousPatterns)
        {
            if (pattern.IsMatch(content))
            {
                errors.Add("Testimonials should focus on your experience and not contain contact information, URLs, or promotional content.");
                break;
            }
        }

        // Check for excessive repetition (potential spam)
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 10)
        {
            var wordCounts = words.GroupBy(w => w.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.Count());
            var maxRepetition = wordCounts.Values.Max();
            var repetitionPercentage = (double)maxRepetition / words.Length;
            
            if (repetitionPercentage > 0.3) // More than 30% repetition
            {
                errors.Add("Please provide a more natural testimonial without excessive word repetition.");
            }
        }

        // Check for construction-related content relevance
        var hasRelevantContent = RequiredWords.Any(word => lowerContent.Contains(word));
        if (!hasRelevantContent && words.Length > 20)
        {
            errors.Add("Please ensure your testimonial relates to construction services and your experience with our work.");
        }

        // Check for all caps (shouting)
        var upperCaseRatio = content.Count(char.IsUpper) / (double)content.Length;
        if (upperCaseRatio > 0.5 && content.Length > 20)
        {
            errors.Add("Please avoid writing in all capital letters.");
        }

        // Check for excessive punctuation
        var punctuationCount = content.Count(c => "!?.,;:".Contains(c));
        if (punctuationCount > content.Length * 0.1) // More than 10% punctuation
        {
            errors.Add("Please use normal punctuation in your testimonial.");
        }

        if (errors.Any())
        {
            return new ValidationResult(string.Join(" ", errors));
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates that the testimonial has sufficient meaningful content
/// </summary>
public class MeaningfulContentAttribute : ValidationAttribute
{
    private readonly int _minimumWords;
    private readonly int _minimumUniqueWords;

    public MeaningfulContentAttribute(int minimumWords = 8, int minimumUniqueWords = 6)
    {
        _minimumWords = minimumWords;
        _minimumUniqueWords = minimumUniqueWords;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string content || string.IsNullOrWhiteSpace(content))
        {
            return ValidationResult.Success; // Let Required attribute handle empty content
        }

        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2) // Ignore very short words
            .Select(w => w.ToLowerInvariant().Trim('.', ',', '!', '?', ';', ':'))
            .Where(w => !string.IsNullOrEmpty(w))
            .ToArray();

        var uniqueWords = words.Distinct().ToArray();

        if (words.Length < _minimumWords)
        {
            return new ValidationResult($"Please provide a more detailed testimonial with at least {_minimumWords} meaningful words. Current: {words.Length} words.");
        }

        if (uniqueWords.Length < _minimumUniqueWords)
        {
            return new ValidationResult($"Please provide more varied content. Your testimonial should contain at least {_minimumUniqueWords} different meaningful words.");
        }

        // Check for generic/template content
        var genericPhrases = new[]
        {
            "great work", "good job", "excellent service", "highly recommend",
            "very professional", "on time", "quality work", "fair price",
            "would use again", "satisfied customer", "job well done"
        };

        var contentLower = content.ToLowerInvariant();
        var genericMatches = genericPhrases.Count(phrase => contentLower.Contains(phrase));
        
        if (genericMatches >= 3 && words.Length < 25)
        {
            return new ValidationResult("Please provide a more specific and personal testimonial about your unique experience. Avoid generic phrases and share specific details about what made your experience special.");
        }

        // Check for minimum sentence structure
        var sentences = content.Split('.', '!', '?').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (sentences.Length < 2 && words.Length > 15)
        {
            return new ValidationResult("Please structure your testimonial in complete sentences for better readability.");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates professional name format
/// </summary>
public class ProfessionalNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Success;
        }

        // Check for minimum parts (first and last name)
        var nameParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length < 2)
        {
            return new ValidationResult("Please provide your full name (first and last name).");
        }

        // Check for suspicious patterns
        if (name.Any(char.IsDigit))
        {
            return new ValidationResult("Names should not contain numbers.");
        }

        // Check for excessive punctuation or special characters
        var specialCharCount = name.Count(c => !char.IsLetter(c) && !char.IsWhiteSpace(c) && c != '.' && c != '-' && c != '\'');
        if (specialCharCount > 2)
        {
            return new ValidationResult("Please provide a valid name using only letters, spaces, periods, hyphens, and apostrophes.");
        }

        // Check for minimum length per name part
        if (nameParts.Any(part => part.Length < 2))
        {
            return new ValidationResult("Each part of your name should be at least 2 characters long.");
        }

        return ValidationResult.Success;
    }
}