using KfConstructionWeb.Models;

namespace KfConstructionWeb.Services.Interfaces;

/// <summary>
/// Service for handling email communications
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send testimonial notification to admin
    /// </summary>
    Task<bool> SendTestimonialNotificationAsync(Testimonial testimonial);
    
    /// <summary>
    /// Send testimonial confirmation to client
    /// </summary>
    Task<bool> SendTestimonialConfirmationAsync(string email, string name);
    
    /// <summary>
    /// Send contact form submission
    /// </summary>
    Task<bool> SendContactFormAsync(string name, string email, string subject, string message);
    
    /// <summary>
    /// Send welcome email to new clients
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string email, string name);
    
    /// <summary>
    /// Send password reset email
    /// </summary>
    Task<bool> SendPasswordResetAsync(string email, string resetLink);
}