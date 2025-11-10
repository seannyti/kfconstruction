using KfConstructionWeb.Models;
using KfConstructionWeb.Models.DTOs;

namespace KfConstructionWeb.Services.Interfaces;

public interface ITestimonialService
{
    // Submission
    Task<TestimonialSubmissionResult> SubmitTestimonialSecureAsync(
        TestimonialSubmissionRequest request,
        string? ipAddress = null,
        string? userAgent = null);
        
    Task<bool> SubmitTestimonialAsync(Testimonial testimonial, string? ipAddress = null, string? userAgent = null);

    // Retrieval - Public
    Task<IEnumerable<Testimonial>> GetPublishedTestimonialsAsync(ServiceType? serviceType = null, bool featuredOnly = false);
    Task<IEnumerable<Testimonial>> GetFeaturedTestimonialsAsync(int count = 3);
    Task<Testimonial?> GetTestimonialByIdAsync(int id);
    Task<IEnumerable<Testimonial>> GetTestimonialsByClientAsync(int clientId);

    // Statistics
    Task<double> GetAverageRatingAsync(ServiceType? serviceType = null);
    Task<Dictionary<int, int>> GetRatingDistributionAsync();

    // Admin Management
    Task<IEnumerable<Testimonial>> GetAllTestimonialsAsync(TestimonialStatus? status = null);
    Task<bool> UpdateTestimonialStatusAsync(int id, TestimonialStatus status, string? adminNotes = null);
    Task<bool> SetFeaturedStatusAsync(int id, bool isFeatured);
    Task<bool> UpdateDisplayOrderAsync(int id, int displayOrder);
    Task<bool> DeleteTestimonialAsync(int id);
    Task<bool> ArchiveTestimonialAsync(int id);
}
