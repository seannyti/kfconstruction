using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class EmailTestController : Controller
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailTestController> _logger;

    public EmailTestController(IEmailService emailService, ILogger<EmailTestController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Email testing page
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Send test email
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendTest(string emailType, string toEmail)
    {
        try
        {
            bool success = false;

            switch (emailType.ToLower())
            {
                case "welcome":
                    success = await _emailService.SendWelcomeEmailAsync(toEmail, "Test User");
                    break;

                case "confirmation":
                    success = await _emailService.SendTestimonialConfirmationAsync(toEmail, "Test User");
                    break;

                case "contact":
                    success = await _emailService.SendContactFormAsync("Test User", toEmail, "Test Subject", "This is a test contact form message.");
                    break;

                case "passwordreset":
                    success = await _emailService.SendPasswordResetAsync(toEmail, "https://example.com/reset-password?token=test123");
                    break;

                default:
                    TempData["ErrorMessage"] = "Invalid email type selected.";
                    return RedirectToAction(nameof(Index));
            }

            if (success)
            {
                TempData["SuccessMessage"] = $"Test {emailType} email sent successfully to {toEmail}!";
                _logger.LogInformation("Test {EmailType} email sent to {Email}", emailType, toEmail);
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to send test {emailType} email to {toEmail}.";
                _logger.LogWarning("Failed to send test {EmailType} email to {Email}", emailType, toEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test {EmailType} email to {Email}", emailType, toEmail);
            TempData["ErrorMessage"] = $"An error occurred while sending the test email: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}