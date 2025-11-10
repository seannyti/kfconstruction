using Microsoft.EntityFrameworkCore;
using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.DTOs;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

/// <summary>
/// Service for managing testimonials and reviews
/// </summary>
public class TestimonialService : ITestimonialService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TestimonialService> _logger;
    private readonly ITestimonialRateLimitService _rateLimitService;

    public TestimonialService(
        ApplicationDbContext context, 
        ILogger<TestimonialService> logger,
        ITestimonialRateLimitService rateLimitService)
    {
        _context = context;
        _logger = logger;
        _rateLimitService = rateLimitService;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Testimonial>> GetPublishedTestimonialsAsync(ServiceType? serviceType = null, bool featuredOnly = false)
    {
        try
        {
            IQueryable<Testimonial> query = _context.Testimonials
                .Where(t => t.Status == TestimonialStatus.Approved)
                .Include(t => t.Client);

            if (serviceType.HasValue)
            {
                query = query.Where(t => t.ServiceType == serviceType);
            }

            if (featuredOnly)
            {
                query = query.Where(t => t.IsFeatured);
            }

            return await query
                .OrderBy(t => t.DisplayOrder)
                .ThenByDescending(t => t.PublishedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving published testimonials");
            return Enumerable.Empty<Testimonial>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Testimonial>> GetFeaturedTestimonialsAsync(int count = 3)
    {
        try
        {
            return await _context.Testimonials
                .Where(t => t.Status == TestimonialStatus.Approved && t.IsFeatured)
                .Include(t => t.Client)
                .OrderBy(t => t.DisplayOrder)
                .ThenByDescending(t => t.PublishedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured testimonials");
            return Enumerable.Empty<Testimonial>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Testimonial>> GetAllTestimonialsAsync(TestimonialStatus? status = null)
    {
        try
        {
            IQueryable<Testimonial> query = _context.Testimonials
                .Include(t => t.Client);

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status);
            }

            return await query
                .OrderByDescending(t => t.SubmittedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all testimonials");
            return Enumerable.Empty<Testimonial>();
        }
    }

    /// <inheritdoc />
    public async Task<Testimonial?> GetTestimonialByIdAsync(int id)
    {
        try
        {
            return await _context.Testimonials
                .Include(t => t.Client)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving testimonial with ID {Id}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TestimonialSubmissionResult> SubmitTestimonialSecureAsync(TestimonialSubmissionRequest request, string? ipAddress = null, string? userAgent = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new TestimonialSubmissionResult
        {
            SecurityAnalysis = new SecurityAnalysisResult()
        };

        try
        {
            // Basic validation
            if (request == null)
            {
                result.Message = "Invalid request data";
                result.ValidationErrors.Add("Request", new List<string> { "Request data is required" });
                return result;
            }

            // Honeypot check (anti-bot measure)
            if (!string.IsNullOrEmpty(request.Website))
            {
                _logger.LogWarning("Honeypot field filled for IP {IpAddress}, likely spam bot", ipAddress);
                result.Message = "Submission rejected";
                result.SecurityAnalysis.SpamDetected = true;
                result.SecurityAnalysis.SecurityFlags.Add("Honeypot field filled");
                result.SecurityAnalysis.RiskLevel = "High";
                return result;
            }

            // Set request metadata
            request.IpAddress = ipAddress;
            request.SubmissionTime = DateTime.UtcNow;

            // Rate limiting check
            if (!string.IsNullOrEmpty(ipAddress))
            {
                var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(ipAddress);
                if (!rateLimitResult.IsAllowed)
                {
                    result.Message = rateLimitResult.Message;
                    result.SecurityAnalysis.RateLimited = true;
                    result.SecurityAnalysis.SecurityFlags.Add("Rate limit exceeded");
                    result.SecurityAnalysis.RiskLevel = "High";
                    return result;
                }
                result.SecurityAnalysis.AnalysisData.Add("RateLimit", rateLimitResult);
            }

            // Duplicate content check
            var duplicateResult = await _rateLimitService.CheckDuplicateContentAsync(request.Content, request.Email);
            if (duplicateResult.IsDuplicate)
            {
                result.Message = duplicateResult.Message;
                result.SecurityAnalysis.DuplicateContent = true;
                result.SecurityAnalysis.SecurityFlags.Add("Duplicate content detected");
                result.SecurityAnalysis.RiskLevel = "Medium";
                return result;
            }
            result.SecurityAnalysis.AnalysisData.Add("DuplicateCheck", duplicateResult);

            // Advanced spam detection
            var spamResult = await _rateLimitService.PerformAdvancedSpamDetectionAsync(request.Content, request.Email, ipAddress);
            result.SecurityAnalysis.SpamDetected = spamResult.IsSpam;
            result.SecurityAnalysis.SecurityScore = spamResult.SpamScore;
            result.SecurityAnalysis.SecurityFlags.AddRange(spamResult.Reasons);
            result.SecurityAnalysis.RiskLevel = spamResult.RiskLevel.ToString();
            result.SecurityAnalysis.AnalysisData.Add("SpamDetection", spamResult);

            if (spamResult.IsSpam)
            {
                result.Message = "Content appears to be spam and was rejected";
                _logger.LogWarning("Spam detected in testimonial submission from IP {IpAddress}. Score: {SpamScore}", 
                    ipAddress, spamResult.SpamScore);
                return result;
            }

            // Create testimonial entity
            var testimonial = new Testimonial
            {
                Name = request.Name,
                Email = request.Email,
                Company = request.Company,
                JobTitle = request.Position,
                Content = request.Content,
                Rating = request.Rating,
                Status = spamResult.SpamScore < 0.3 ? TestimonialStatus.Approved : TestimonialStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent
            };

            if (spamResult.SpamScore < 0.3)
            {
                testimonial.PublishedAt = DateTime.UtcNow;
            }

            result.RequiresApproval = testimonial.Status == TestimonialStatus.Pending;

            // Submit the testimonial
            var success = await SubmitTestimonialAsync(testimonial, ipAddress, userAgent);

            if (success)
            {
                // Record successful submission for rate limiting and duplicate detection
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    await _rateLimitService.RecordSubmissionAsync(ipAddress);
                }
                await _rateLimitService.RecordContentAsync(request.Content, request.Email);

                result.Success = true;
                result.TestimonialId = testimonial.Id;
                result.Message = testimonial.Status == TestimonialStatus.Approved
                    ? "Thank you for your testimonial! It has been published."
                    : "Thank you for your testimonial! It has been submitted for review and will be published soon.";
                
                _logger.LogInformation("Secure testimonial submission successful for {Name} from IP {IpAddress}. Status: {Status}", 
                    request.Name, ipAddress, testimonial.Status);
            }
            else
            {
                result.Message = "There was an error submitting your testimonial. Please try again.";
                result.ErrorDetails = "Database save operation failed";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in secure testimonial submission for IP {IpAddress}", ipAddress);
            result.Message = "An unexpected error occurred. Please try again later.";
            result.ErrorDetails = ex.Message;
            return result;
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SubmitTestimonialAsync(Testimonial testimonial, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            testimonial.SubmittedAt = DateTime.UtcNow;
            testimonial.CreatedAt = DateTime.UtcNow;
            testimonial.UpdatedAt = DateTime.UtcNow;
            testimonial.Status = TestimonialStatus.Pending;
            testimonial.IPAddress = ipAddress;
            testimonial.UserAgent = userAgent;

            _context.Testimonials.Add(testimonial);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation("New testimonial submitted by {Name} for review", testimonial.Name);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting testimonial");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateTestimonialStatusAsync(int id, TestimonialStatus status, string? adminNotes = null)
    {
        try
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null)
            {
                _logger.LogWarning("Testimonial with ID {Id} not found for status update", id);
                return false;
            }

            testimonial.Status = status;
            testimonial.UpdatedAt = DateTime.UtcNow;
            testimonial.AdminNotes = adminNotes;

            if (status == TestimonialStatus.Approved && !testimonial.PublishedAt.HasValue)
            {
                testimonial.PublishedAt = DateTime.UtcNow;
            }

            var result = await _context.SaveChangesAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("Testimonial {Id} status updated to {Status}", id, status);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating testimonial status for ID {Id}", id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetFeaturedStatusAsync(int id, bool isFeatured)
    {
        try
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null)
            {
                _logger.LogWarning("Testimonial with ID {Id} not found for featured status update", id);
                return false;
            }

            testimonial.IsFeatured = isFeatured;
            testimonial.UpdatedAt = DateTime.UtcNow;

            // If setting as featured and no display order set, put it first
            if (isFeatured && testimonial.DisplayOrder == 0)
            {
                testimonial.DisplayOrder = 1;
            }

            var result = await _context.SaveChangesAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("Testimonial {Id} featured status set to {IsFeatured}", id, isFeatured);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating featured status for testimonial ID {Id}", id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDisplayOrderAsync(int id, int displayOrder)
    {
        try
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null)
            {
                _logger.LogWarning("Testimonial with ID {Id} not found for display order update", id);
                return false;
            }

            testimonial.DisplayOrder = displayOrder;
            testimonial.UpdatedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("Testimonial {Id} display order updated to {DisplayOrder}", id, displayOrder);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating display order for testimonial ID {Id}", id);
            return false;
        }
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<IEnumerable<Testimonial>> GetTestimonialsByClientAsync(int clientId)
    {
        try
        {
            return await _context.Testimonials
                .Where(t => t.ClientId == clientId)
                .OrderByDescending(t => t.SubmittedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving testimonials for client {ClientId}", clientId);
            return Enumerable.Empty<Testimonial>();
        }
    }

    /// <inheritdoc />
    public async Task<double> GetAverageRatingAsync(ServiceType? serviceType = null)
    {
        try
        {
            IQueryable<Testimonial> query = _context.Testimonials
                .Where(t => t.Status == TestimonialStatus.Approved);

            if (serviceType.HasValue)
            {
                query = query.Where(t => t.ServiceType == serviceType);
            }

            var ratings = await query.Select(t => t.Rating).ToListAsync();
            
            return ratings.Any() ? ratings.Average() : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating average rating");
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, int>> GetRatingDistributionAsync()
    {
        try
        {
            var ratings = await _context.Testimonials
                .Where(t => t.Status == TestimonialStatus.Approved)
                .GroupBy(t => t.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            var distribution = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++)
            {
                distribution[i] = ratings.FirstOrDefault(r => r.Rating == i)?.Count ?? 0;
            }

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating distribution");
            return new Dictionary<int, int>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTestimonialAsync(int id)
    {
        try
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null)
            {
                _logger.LogWarning("Testimonial with ID {Id} not found for deletion", id);
                return false;
            }

            _context.Testimonials.Remove(testimonial);
            var result = await _context.SaveChangesAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("Testimonial {Id} deleted", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting testimonial ID {Id}", id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ArchiveTestimonialAsync(int id)
    {
        try
        {
            var testimonial = await _context.Testimonials.FindAsync(id);
            if (testimonial == null)
            {
                _logger.LogWarning("Testimonial with ID {Id} not found for archiving", id);
                return false;
            }

            testimonial.Status = TestimonialStatus.Archived;
            testimonial.UpdatedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("Testimonial {Id} archived", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving testimonial ID {Id}", id);
            return false;
        }
    }
}
