using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Text;
using KfConstructionWeb.Models.Configuration;
using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

/// <summary>
/// Enhanced email service with SMTP support and logging
/// </summary>
public partial class EmailSender : IEmailSender, IEmailService
{
    private readonly ILogger<EmailSender> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly ApplicationDbContext _context;
    private readonly ISettingsService _settingsService;

    public EmailSender(
        ILogger<EmailSender> logger, 
        IOptions<EmailSettings> emailSettings,
        ApplicationDbContext context,
        ISettingsService settingsService)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
        _context = context;
        _settingsService = settingsService;
    }

    #region IEmailSender Implementation (for Identity)

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await SendEmailInternalAsync(
            email, 
            _emailSettings.DefaultSender.Email, 
            _emailSettings.DefaultSender.Name,
            subject, 
            htmlMessage, 
            "Identity");
    }

    #endregion

    #region IEmailService Implementation (for Business Logic)

    public async Task<bool> SendTestimonialNotificationAsync(Testimonial testimonial)
    {
        try
        {
            var subject = "New Testimonial Submitted - Review Required";
            var body = GenerateTestimonialNotificationHtml(testimonial);
            
            var success = await SendEmailInternalAsync(
                _emailSettings.AdminNotifications.Email,
                _emailSettings.DefaultSender.Email,
                _emailSettings.DefaultSender.Name,
                subject,
                body,
                "TestimonialNotification",
                testimonial.Id);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send testimonial notification for testimonial {TestimonialId}", testimonial.Id);
            return false;
        }
    }

    public async Task<bool> SendTestimonialConfirmationAsync(string email, string name)
    {
        try
        {
            var subject = "Thank You for Your Testimonial - KF Construction";
            var body = GenerateTestimonialConfirmationHtml(name);
            
            var success = await SendEmailInternalAsync(
                email,
                _emailSettings.DefaultSender.Email,
                _emailSettings.DefaultSender.Name,
                subject,
                body,
                "TestimonialConfirmation");

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send testimonial confirmation to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendContactFormAsync(string name, string email, string subject, string message)
    {
        try
        {
            var emailSubject = $"Contact Form Submission: {subject}";
            var body = GenerateContactFormHtml(name, email, subject, message);
            
            var success = await SendEmailInternalAsync(
                _emailSettings.AdminNotifications.Email,
                _emailSettings.DefaultSender.Email,
                _emailSettings.DefaultSender.Name,
                emailSubject,
                body,
                "ContactForm");

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact form from {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string name)
    {
        try
        {
            var subject = "Welcome to KF Construction Client Portal";
            var body = GenerateWelcomeEmailHtml(name);
            
            var success = await SendEmailInternalAsync(
                email,
                _emailSettings.DefaultSender.Email,
                _emailSettings.DefaultSender.Name,
                subject,
                body,
                "Welcome");

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetAsync(string email, string resetLink)
    {
        try
        {
            var subject = "Password Reset - KF Construction";
            var body = GeneratePasswordResetHtml(resetLink);
            
            var success = await SendEmailInternalAsync(
                email,
                _emailSettings.DefaultSender.Email,
                _emailSettings.DefaultSender.Name,
                subject,
                body,
                "PasswordReset");

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            return false;
        }
    }

    #endregion

    #region Private Methods

    private async Task<bool> SendEmailInternalAsync(
        string toEmail, 
        string fromEmail, 
        string fromName, 
        string subject, 
        string htmlBody, 
        string emailType, 
        int? relatedEntityId = null)
    {
        // Create email log entry
        var emailLog = new EmailLog
        {
            ToEmail = toEmail,
            FromEmail = fromEmail,
            Subject = subject,
            BodyHtml = htmlBody,
            EmailType = emailType,
            RelatedEntityId = relatedEntityId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Check if emails are enabled in database settings (overrides appsettings.json)
            var enableEmailsSetting = await _settingsService.GetSettingAsync("Email.EnableEmails", true);
            
            _logger.LogInformation("Email.EnableEmails setting value: {EnableEmails}", enableEmailsSetting);
            
            if (!enableEmailsSetting)
            {
                _logger.LogWarning("Email sending is disabled via admin settings. Email logged only.");
                emailLog.Status = "Disabled";
                emailLog.ErrorMessage = "Email sending is disabled in admin configuration";
                await LogEmailAsync(emailLog);
                return false;
            }

            // Try to get SMTP settings from database first, fall back to config
            var smtpHost = await _settingsService.GetSettingAsync("Email.SmtpServer", _emailSettings.Smtp.Host);
            var smtpPort = await _settingsService.GetSettingAsync("Email.SmtpPort", _emailSettings.Smtp.Port);
            var smtpUsername = await _settingsService.GetSettingAsync("Email.SmtpUsername", _emailSettings.Smtp.Username);
            var smtpPassword = await _settingsService.GetSettingAsync("Email.SmtpPassword", _emailSettings.Smtp.Password);
            var useSSL = await _settingsService.GetSettingAsync("Email.UseSSL", _emailSettings.Smtp.EnableSsl);

            // Validate SMTP configuration (from database or config)
            if (string.IsNullOrWhiteSpace(smtpHost) || 
                string.IsNullOrWhiteSpace(smtpUsername) ||
                string.IsNullOrWhiteSpace(smtpPassword))
            {
                _logger.LogError("SMTP configuration is incomplete. Host: '{Host}', Username: '{Username}', Password: {HasPassword}", 
                    smtpHost ?? "NOT SET", 
                    smtpUsername ?? "NOT SET",
                    !string.IsNullOrWhiteSpace(smtpPassword) ? "SET" : "NOT SET");
                
                emailLog.Status = "Failed";
                emailLog.ErrorMessage = "SMTP configuration is incomplete. Please configure SMTP settings in Admin Settings or appsettings.json (Host, Username, Password are required)";
                await LogEmailAsync(emailLog);
                return false;
            }

            // Allow DevelopmentMode override from admin settings
            var developmentMode = await _settingsService.GetSettingAsync("Email.DevelopmentMode", _emailSettings.DevelopmentMode);

            if (developmentMode)
            {
                // Development mode - just log
                _logger.LogInformation("=== EMAIL (Development Mode) ===");
                _logger.LogInformation("To: {ToEmail}", toEmail);
                _logger.LogInformation("From: {FromEmail} ({FromName})", fromEmail, fromName);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Type: {EmailType}", emailType);
                _logger.LogInformation("Body: {Body}", htmlBody);
                _logger.LogInformation("=====================================");
                
                emailLog.Status = "Sent";
                emailLog.SentAt = DateTime.UtcNow;
                await LogEmailAsync(emailLog);
                return true;
            }

            // Production mode - actually send email using database settings or config fallback
            await SendViaSmtpAsync(toEmail, fromEmail, fromName, subject, htmlBody, smtpHost, smtpPort, smtpUsername, smtpPassword, useSSL);
            
            emailLog.Status = "Sent";
            emailLog.SentAt = DateTime.UtcNow;
            await LogEmailAsync(emailLog);
            
            _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} with subject '{Subject}'", toEmail, subject);
            
            emailLog.Status = "Failed";
            emailLog.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            await LogEmailAsync(emailLog);
            
            return false;
        }
    }

    private async Task SendViaSmtpAsync(string toEmail, string fromEmail, string fromName, string subject, string htmlBody, 
        string smtpHost, int smtpPort, string smtpUsername, string smtpPassword, bool enableSsl)
    {
        using var client = new SmtpClient(smtpHost, smtpPort);
        client.EnableSsl = enableSsl;
        
        if (!string.IsNullOrEmpty(smtpUsername))
        {
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
        }

        using var message = new MailMessage();
        message.From = new MailAddress(fromEmail, fromName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;
        message.BodyEncoding = Encoding.UTF8;

        await client.SendMailAsync(message);
    }

    private async Task LogEmailAsync(EmailLog emailLog)
    {
        try
        {
            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log email to database");
        }
    }

    #endregion
}