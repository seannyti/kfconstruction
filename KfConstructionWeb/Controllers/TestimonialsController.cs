using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.DTOs;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Controllers;

/// <summary>
/// Controller for managing testimonials and reviews
/// </summary>
public class TestimonialsController : BaseController
{
    private readonly ITestimonialService _testimonialService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<TestimonialsController> _logger;
    private readonly IEmailService _emailService;

    public TestimonialsController(
        ITestimonialService testimonialService,
        UserManager<IdentityUser> userManager,
        ILogger<TestimonialsController> logger,
        IEmailService emailService,
        ISiteConfigService siteConfigService) : base(siteConfigService)
    {
        _testimonialService = testimonialService;
        _userManager = userManager;
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// Display public testimonials page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(ServiceType? serviceType = null)
    {
        try
        {
            var testimonials = await _testimonialService.GetPublishedTestimonialsAsync(serviceType);
            var averageRating = await _testimonialService.GetAverageRatingAsync(serviceType);
            var ratingDistribution = await _testimonialService.GetRatingDistributionAsync();

            ViewBag.ServiceType = serviceType;
            ViewBag.AverageRating = Math.Round(averageRating, 1);
            ViewBag.RatingDistribution = ratingDistribution;
            ViewBag.TotalReviews = testimonials.Count();

            return View(testimonials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading testimonials page");
            return View(new List<Testimonial>());
        }
    }

    /// <summary>
    /// Show testimonial submission form
    /// </summary>
    [HttpGet]
    public IActionResult Submit()
    {
        var testimonial = new Testimonial();
        
        // Pre-populate with user info if logged in
        if (User.Identity?.IsAuthenticated == true)
        {
            // Note: In a real application, you might want to get additional user details
            testimonial.Email = User.Identity.Name;
        }

        return View(testimonial);
    }

    /// <summary>
    /// Process testimonial submission with enhanced security
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Testimonial testimonial, string? honeypot = null, long timestamp = 0)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(testimonial);
            }

            // Create secure submission request
            var request = new TestimonialSubmissionRequest
            {
                Name = testimonial.Name ?? string.Empty,
                Email = testimonial.Email ?? string.Empty,
                Company = testimonial.Company,
                Position = testimonial.JobTitle,
                Content = testimonial.Content ?? string.Empty,
                Rating = testimonial.Rating,
                Website = honeypot // Honeypot field
            };

            // Get client info from current request
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            // If user is logged in, try to associate with client
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Note: You might want to implement logic to find the Client entity
                    // associated with the current user here
                }
            }

            var result = await _testimonialService.SubmitTestimonialSecureAsync(request, ipAddress, userAgent);

            // Handle submission result
            if (result.Success)
            {
                // Send email notifications
                try
                {
                    // Send admin notification if testimonial was created
                    if (result.TestimonialId.HasValue)
                    {
                        var createdTestimonial = await _testimonialService.GetTestimonialByIdAsync(result.TestimonialId.Value);
                        if (createdTestimonial != null)
                        {
                            await _emailService.SendTestimonialNotificationAsync(createdTestimonial);
                            _logger.LogInformation("Admin notification sent for testimonial {TestimonialId}", result.TestimonialId.Value);
                        }
                    }

                    // Send confirmation to customer
                    if (!string.IsNullOrEmpty(request.Email) && !string.IsNullOrEmpty(request.Name))
                    {
                        await _emailService.SendTestimonialConfirmationAsync(request.Email, request.Name);
                        _logger.LogInformation("Confirmation email sent to {Email}", request.Email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notifications for testimonial submission");
                    // Don't fail the submission if email fails
                }

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            // Handle different error types based on security analysis
            if (result.SecurityAnalysis?.RateLimited == true)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            
            if (result.SecurityAnalysis?.DuplicateContent == true)
            {
                ModelState.AddModelError("Content", result.Message);
            }
            else if (result.SecurityAnalysis?.SpamDetected == true)
            {
                _logger.LogWarning("Spam detected in testimonial submission from IP {IpAddress}", ipAddress);
                // Don't reveal spam detection to potential spammers
                TempData["ErrorMessage"] = "There was an issue with your submission. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            else if (result.ValidationErrors.Any())
            {
                foreach (var error in result.ValidationErrors)
                {
                    foreach (var message in error.Value)
                    {
                        ModelState.AddModelError(error.Key, message);
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", result.Message);
            }

            return View(testimonial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting testimonial");
            ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
            return View(testimonial);
        }
    }

    /// <summary>
    /// Display detailed view of a specific testimonial
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var testimonial = await _testimonialService.GetTestimonialByIdAsync(id);
            
            if (testimonial == null || testimonial.Status != TestimonialStatus.Approved)
            {
                return NotFound();
            }

            return View(testimonial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading testimonial details for ID {Id}", id);
            return NotFound();
        }
    }

    #region Admin Actions

    /// <summary>
    /// Admin page for managing all testimonials
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Manage(TestimonialStatus? status = null)
    {
        try
        {
            var testimonials = await _testimonialService.GetAllTestimonialsAsync(status);
            ViewBag.FilterStatus = status;
            return View(testimonials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading testimonials management page");
            return View(new List<Testimonial>());
        }
    }

    /// <summary>
    /// Update testimonial status (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, TestimonialStatus status, string? adminNotes = null)
    {
        try
        {
            var success = await _testimonialService.UpdateTestimonialStatusAsync(id, status, adminNotes);
            
            if (success)
            {
                TempData["SuccessMessage"] = $"Testimonial status updated to {status}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update testimonial status.";
            }

            return RedirectToAction(nameof(Manage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating testimonial status for ID {Id}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the testimonial status.";
            return RedirectToAction(nameof(Manage));
        }
    }

    /// <summary>
    /// Toggle featured status for testimonial (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(int id)
    {
        try
        {
            var testimonial = await _testimonialService.GetTestimonialByIdAsync(id);
            if (testimonial == null)
            {
                return NotFound();
            }

            var success = await _testimonialService.SetFeaturedStatusAsync(id, !testimonial.IsFeatured);
            
            if (success)
            {
                var status = !testimonial.IsFeatured ? "featured" : "unfeatured";
                TempData["SuccessMessage"] = $"Testimonial has been {status}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update featured status.";
            }

            return RedirectToAction(nameof(Manage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling featured status for testimonial ID {Id}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the featured status.";
            return RedirectToAction(nameof(Manage));
        }
    }

    /// <summary>
    /// Update display order for testimonial (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDisplayOrder(int id, int displayOrder)
    {
        try
        {
            var success = await _testimonialService.UpdateDisplayOrderAsync(id, displayOrder);
            
            if (success)
            {
                return Json(new { success = true, message = "Display order updated successfully." });
            }

            return Json(new { success = false, message = "Failed to update display order." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating display order for testimonial ID {Id}", id);
            return Json(new { success = false, message = "An error occurred while updating the display order." });
        }
    }

    /// <summary>
    /// Delete testimonial (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _testimonialService.DeleteTestimonialAsync(id);
            
            if (success)
            {
                TempData["SuccessMessage"] = "Testimonial has been deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete testimonial.";
            }

            return RedirectToAction(nameof(Manage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting testimonial ID {Id}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the testimonial.";
            return RedirectToAction(nameof(Manage));
        }
    }

    /// <summary>
    /// Archive testimonial instead of deleting (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        try
        {
            var success = await _testimonialService.ArchiveTestimonialAsync(id);
            
            if (success)
            {
                TempData["SuccessMessage"] = "Testimonial has been archived.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to archive testimonial.";
            }

            return RedirectToAction(nameof(Manage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving testimonial ID {Id}", id);
            TempData["ErrorMessage"] = "An error occurred while archiving the testimonial.";
            return RedirectToAction(nameof(Manage));
        }
    }

    #endregion
}