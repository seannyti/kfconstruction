using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Services.Interfaces;
using KfConstructionWeb.Models.Configuration;
using Microsoft.Extensions.Options;
using KfConstructionWeb.Models.ViewModels;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class EmailController : Controller
{
    private readonly ISettingsService _settingsService;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        ISettingsService settingsService,
        IEmailService emailService,
        IOptions<EmailSettings> emailOptions,
        ILogger<EmailController> logger)
    {
        _settingsService = settingsService;
        _emailService = emailService;
        _emailSettings = emailOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EmailConfigViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.LastResult = "Validation failed.";
            return View("Index", model);
        }

        try
        {
            await _settingsService.SetSettingAsync("Email.SmtpServer", model.Host ?? string.Empty, "Email", "SMTP Host");
            await _settingsService.SetSettingAsync("Email.SmtpPort", model.Port.ToString(), "Email", "SMTP Port");
            if (!string.IsNullOrWhiteSpace(model.Username))
                await _settingsService.SetSettingAsync("Email.SmtpUsername", model.Username, "Email", "SMTP Username");
            if (!string.IsNullOrWhiteSpace(model.Password))
                await _settingsService.SetSettingAsync("Email.SmtpPassword", model.Password, "Email", "SMTP Password (stored encrypted if implemented)");
            await _settingsService.SetSettingAsync("Email.UseSSL", model.UseSsl.ToString(), "Email", "Use SSL/TLS");
            await _settingsService.SetSettingAsync("Email.EnableEmails", model.EnableEmails.ToString(), "Email", "Enable actual sending");
            await _settingsService.SetSettingAsync("Email.DevelopmentMode", model.DevelopmentMode.ToString(), "Email", "Log only, no send");

            TempData["Success"] = "Email configuration saved.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save email configuration");
            TempData["Error"] = "Failed to save email configuration.";
        }

        var vm = await BuildViewModelAsync();
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestSend(EmailConfigViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.TestRecipient))
        {
            TempData["Error"] = "Test recipient email is required.";
            var vmMissing = await BuildViewModelAsync();
            return View("Index", vmMissing);
        }

        // Simple test body
        var subject = "Email Service Diagnostic Test";
        var body = $"<h2>KF Construction Email Diagnostic</h2><p>Timestamp (UTC): {DateTime.UtcNow}</p><p>Host: {model.Host}</p><p>Port: {model.Port}</p><p>SSL: {model.UseSsl}</p><p>DevelopmentMode: {model.DevelopmentMode}</p>";

        bool sent;
        try
        {
            // Direct internal call to bypass business wrappers
            sent = await _emailService.SendWelcomeEmailAsync(model.TestRecipient!, "Diagnostic User");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test send threw exception");
            sent = false;
        }

        TempData[sent ? "Success" : "Error"] = sent ? "Test email sent (or logged if development mode)." : "Test email failed. Check logs.";

        var refreshed = await BuildViewModelAsync();
        refreshed.TestRecipient = model.TestRecipient;
        return View("Index", refreshed);
    }

    private async Task<EmailConfigViewModel> BuildViewModelAsync()
    {
        var host = await _settingsService.GetSettingAsync("Email.SmtpServer") ?? _emailSettings.Smtp.Host;
        var port = await _settingsService.GetSettingAsync<int>("Email.SmtpPort", _emailSettings.Smtp.Port);
        var username = await _settingsService.GetSettingAsync("Email.SmtpUsername") ?? _emailSettings.Smtp.Username;
        var useSsl = await _settingsService.GetSettingAsync<bool>("Email.UseSSL", _emailSettings.Smtp.EnableSsl);
        var enableEmails = await _settingsService.GetSettingAsync<bool>("Email.EnableEmails", _emailSettings.EnableEmails);
        var developmentMode = await _settingsService.GetSettingAsync<bool>("Email.DevelopmentMode", _emailSettings.DevelopmentMode);

        return new EmailConfigViewModel
        {
            Host = host,
            Port = port,
            Username = username,
            UseSsl = useSsl,
            EnableEmails = enableEmails,
            DevelopmentMode = developmentMode,
            DefaultSenderEmail = _emailSettings.DefaultSender.Email,
            DefaultSenderName = _emailSettings.DefaultSender.Name
        };
    }
}
