using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.ViewComponents;

/// <summary>
/// View component for displaying featured testimonials
/// </summary>
public class FeaturedTestimonialsViewComponent : ViewComponent
{
    private readonly ITestimonialService _testimonialService;
    private readonly ILogger<FeaturedTestimonialsViewComponent> _logger;

    public FeaturedTestimonialsViewComponent(
        ITestimonialService testimonialService,
        ILogger<FeaturedTestimonialsViewComponent> logger)
    {
        _testimonialService = testimonialService;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the view component to display featured testimonials
    /// </summary>
    /// <param name="count">Number of testimonials to display (default: 3)</param>
    /// <returns>View component result</returns>
    public async Task<IViewComponentResult> InvokeAsync(int count = 3)
    {
        try
        {
            var featuredTestimonials = await _testimonialService.GetFeaturedTestimonialsAsync(count);
            var averageRating = await _testimonialService.GetAverageRatingAsync();
            
            ViewBag.AverageRating = Math.Round(averageRating, 1);
            ViewBag.TotalTestimonials = (await _testimonialService.GetPublishedTestimonialsAsync()).Count();
            
            return View(featuredTestimonials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading featured testimonials view component");
            return View(new List<KfConstructionWeb.Models.Testimonial>());
        }
    }
}