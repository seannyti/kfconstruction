using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Models.ViewModels;
using KfConstructionWeb.Services.Interfaces;
using KfConstructionWeb.Controllers;

namespace KfConstructionWeb.Areas.Admin.Controllers;

/// <summary>
/// Handles application settings management for administrators
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SettingsController : BaseController
{
    private readonly ISettingsService _settingsService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingsService settingsService, 
        UserManager<IdentityUser> userManager,
        ILogger<SettingsController> logger,
        ISiteConfigService siteConfigService) : base(siteConfigService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Settings Loading Methods

    /// <summary>
    /// Loads general application settings from the database
    /// </summary>
    /// <param name="model">The general settings view model to populate</param>
    private async Task LoadGeneralSettingsAsync(GeneralSettingsViewModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        model.CompanyName = await _settingsService.GetSettingAsync("General.CompanyName", "KF Construction") ?? "KF Construction";
        model.CompanyEmail = await _settingsService.GetSettingAsync("General.CompanyEmail", "knudsonfamilyconstruction@yahoo.com") ?? "knudsonfamilyconstruction@yahoo.com";
        model.CompanyPhone = await _settingsService.GetSettingAsync("General.CompanyPhone", "") ?? "";
        model.CompanyAddress = await _settingsService.GetSettingAsync("General.CompanyAddress", "") ?? "";
        model.SiteTitle = await _settingsService.GetSettingAsync("General.SiteTitle", "KF Construction Management") ?? "KF Construction Management";
        model.SiteDescription = await _settingsService.GetSettingAsync("General.SiteDescription", "") ?? "";
    }

    /// <summary>
    /// Loads security-related settings from the database
    /// </summary>
    /// <param name="model">The security settings view model to populate</param>
    private async Task LoadSecuritySettingsAsync(SecuritySettingsViewModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        model.RequireEmailConfirmation = await _settingsService.GetSettingAsync("Security.RequireEmailConfirmation", false);
        model.EnableTwoFactorAuth = await _settingsService.GetSettingAsync("Security.EnableTwoFactorAuth", true);
        model.SessionTimeoutMinutes = await _settingsService.GetSettingAsync("Security.SessionTimeoutMinutes", 30);
        model.PasswordMinLength = await _settingsService.GetSettingAsync("Security.PasswordMinLength", 8);
        model.RequirePasswordSpecialChars = await _settingsService.GetSettingAsync("Security.RequirePasswordSpecialChars", true);
        model.MaxLoginAttempts = await _settingsService.GetSettingAsync("Security.MaxLoginAttempts", 5);
    }

    /// <summary>
    /// Loads email configuration settings from the database
    /// </summary>
    /// <param name="model">The email settings view model to populate</param>
    private async Task LoadEmailSettingsAsync(EmailSettingsViewModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        model.EnableEmails = await _settingsService.GetSettingAsync("Email.EnableEmails", true);
        model.SmtpServer = await _settingsService.GetSettingAsync("Email.SmtpServer", "") ?? "";
        model.SmtpPort = await _settingsService.GetSettingAsync("Email.SmtpPort", 587);
        model.SmtpUsername = await _settingsService.GetSettingAsync("Email.SmtpUsername", "") ?? "";
        model.SmtpPassword = await _settingsService.GetSettingAsync("Email.SmtpPassword", "") ?? "";
        model.UseSSL = await _settingsService.GetSettingAsync("Email.UseSSL", true);
        model.FromEmail = await _settingsService.GetSettingAsync("Email.FromEmail", "") ?? "";
        model.FromDisplayName = await _settingsService.GetSettingAsync("Email.FromDisplayName", "") ?? "";
    }

    /// <summary>
    /// Loads maintenance mode and system settings from the database
    /// </summary>
    /// <param name="model">The maintenance settings view model to populate</param>
    private async Task LoadMaintenanceSettingsAsync(MaintenanceSettingsViewModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        model.MaintenanceMode = await _settingsService.GetSettingAsync("Maintenance.MaintenanceMode", false);
        model.MaintenanceMessage = await _settingsService.GetSettingAsync("Maintenance.MaintenanceMessage", "") ?? "";
        model.EnableUserRegistration = await _settingsService.GetSettingAsync("Maintenance.EnableUserRegistration", true);
        model.EnableApiAccess = await _settingsService.GetSettingAsync("Maintenance.EnableApiAccess", true);
    }

    #endregion

    #region Settings Saving Methods

    /// <summary>
    /// Saves general application settings to the database
    /// </summary>
    /// <param name="model">The general settings view model containing values to save</param>
    /// <param name="modifiedBy">The user who is making the changes</param>
    private async Task SaveGeneralSettingsAsync(GeneralSettingsViewModel model, string modifiedBy)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrWhiteSpace(modifiedBy)) throw new ArgumentException("ModifiedBy cannot be null or empty", nameof(modifiedBy));

        await _settingsService.SetSettingAsync("General.CompanyName", model.CompanyName, "General", "Company Name", modifiedBy);
        await _settingsService.SetSettingAsync("General.CompanyEmail", model.CompanyEmail, "General", "Company Email", modifiedBy);
        await _settingsService.SetSettingAsync("General.CompanyPhone", model.CompanyPhone ?? "", "General", "Company Phone", modifiedBy);
        await _settingsService.SetSettingAsync("General.CompanyAddress", model.CompanyAddress ?? "", "General", "Company Address", modifiedBy);
        await _settingsService.SetSettingAsync("General.SiteTitle", model.SiteTitle, "General", "Site Title", modifiedBy);
        await _settingsService.SetSettingAsync("General.SiteDescription", model.SiteDescription ?? "", "General", "Site Description", modifiedBy);
    }

    /// <summary>
    /// Saves security-related settings to the database
    /// </summary>
    /// <param name="model">The security settings view model containing values to save</param>
    /// <param name="modifiedBy">The user who is making the changes</param>
    private async Task SaveSecuritySettingsAsync(SecuritySettingsViewModel model, string modifiedBy)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrWhiteSpace(modifiedBy)) throw new ArgumentException("ModifiedBy cannot be null or empty", nameof(modifiedBy));

        await _settingsService.SetSettingAsync("Security.RequireEmailConfirmation", model.RequireEmailConfirmation.ToString(), "Security", "Require Email Confirmation", modifiedBy);
        await _settingsService.SetSettingAsync("Security.EnableTwoFactorAuth", model.EnableTwoFactorAuth.ToString(), "Security", "Enable Two-Factor Authentication", modifiedBy);
        await _settingsService.SetSettingAsync("Security.SessionTimeoutMinutes", model.SessionTimeoutMinutes.ToString(), "Security", "Session Timeout in Minutes", modifiedBy);
        await _settingsService.SetSettingAsync("Security.PasswordMinLength", model.PasswordMinLength.ToString(), "Security", "Password Minimum Length", modifiedBy);
        await _settingsService.SetSettingAsync("Security.RequirePasswordSpecialChars", model.RequirePasswordSpecialChars.ToString(), "Security", "Require Password Special Characters", modifiedBy);
        await _settingsService.SetSettingAsync("Security.MaxLoginAttempts", model.MaxLoginAttempts.ToString(), "Security", "Maximum Login Attempts", modifiedBy);
    }

    /// <summary>
    /// Saves email configuration settings to the database
    /// </summary>
    /// <param name="model">The email settings view model containing values to save</param>
    /// <param name="modifiedBy">The user who is making the changes</param>
    private async Task SaveEmailSettingsAsync(EmailSettingsViewModel model, string modifiedBy)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrWhiteSpace(modifiedBy)) throw new ArgumentException("ModifiedBy cannot be null or empty", nameof(modifiedBy));

        await _settingsService.SetSettingAsync("Email.EnableEmails", model.EnableEmails.ToString(), "Email", "Enable Emails", modifiedBy);
        await _settingsService.SetSettingAsync("Email.SmtpServer", model.SmtpServer ?? "", "Email", "SMTP Server", modifiedBy);
        await _settingsService.SetSettingAsync("Email.SmtpPort", model.SmtpPort.ToString(), "Email", "SMTP Port", modifiedBy);
        await _settingsService.SetSettingAsync("Email.SmtpUsername", model.SmtpUsername ?? "", "Email", "SMTP Username", modifiedBy);
        await _settingsService.SetSettingAsync("Email.SmtpPassword", model.SmtpPassword ?? "", "Email", "SMTP Password", modifiedBy);
        await _settingsService.SetSettingAsync("Email.UseSSL", model.UseSSL.ToString(), "Email", "Use SSL", modifiedBy);
        await _settingsService.SetSettingAsync("Email.FromEmail", model.FromEmail ?? "", "Email", "From Email Address", modifiedBy);
        await _settingsService.SetSettingAsync("Email.FromDisplayName", model.FromDisplayName ?? "", "Email", "From Display Name", modifiedBy);
    }

    /// <summary>
    /// Saves maintenance mode and system settings to the database
    /// </summary>
    /// <param name="model">The maintenance settings view model containing values to save</param>
    /// <param name="modifiedBy">The user who is making the changes</param>
    private async Task SaveMaintenanceSettingsAsync(MaintenanceSettingsViewModel model, string modifiedBy)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrWhiteSpace(modifiedBy)) throw new ArgumentException("ModifiedBy cannot be null or empty", nameof(modifiedBy));

        await _settingsService.SetSettingAsync("Maintenance.MaintenanceMode", model.MaintenanceMode.ToString(), "Maintenance", "Maintenance Mode", modifiedBy);
        await _settingsService.SetSettingAsync("Maintenance.MaintenanceMessage", model.MaintenanceMessage ?? "", "Maintenance", "Maintenance Message", modifiedBy);
        await _settingsService.SetSettingAsync("Maintenance.EnableUserRegistration", model.EnableUserRegistration.ToString(), "Maintenance", "Enable User Registration", modifiedBy);
        await _settingsService.SetSettingAsync("Maintenance.EnableApiAccess", model.EnableApiAccess.ToString(), "Maintenance", "Enable API Access", modifiedBy);
    }

    #endregion

    #region Action Methods

    /// <summary>
    /// Displays the settings management page
    /// </summary>
    /// <returns>Settings view with current configuration values</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var viewModel = new SettingsViewModel();
            
            // Load all settings using organized helper methods
            await LoadGeneralSettingsAsync(viewModel.General);
            await LoadSecuritySettingsAsync(viewModel.Security);
            await LoadEmailSettingsAsync(viewModel.Email);
            await LoadMaintenanceSettingsAsync(viewModel.Maintenance);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings page");
            TempData["Error"] = "Error loading settings. Please try again.";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    /// <summary>
    /// Processes settings form submission and saves changes
    /// </summary>
    /// <param name="model">The settings view model containing user input</param>
    /// <returns>Redirect to settings page on success, or redisplay form on validation error</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var modifiedBy = currentUser?.Email ?? "System";

            // Save all settings using organized helper methods
            await SaveGeneralSettingsAsync(model.General, modifiedBy);
            await SaveSecuritySettingsAsync(model.Security, modifiedBy);
            await SaveEmailSettingsAsync(model.Email, modifiedBy);
            await SaveMaintenanceSettingsAsync(model.Maintenance, modifiedBy);

            TempData["Success"] = "Settings have been saved successfully.";
            _logger.LogInformation("Settings updated by user {User}", modifiedBy);
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            TempData["Error"] = "Error saving settings. Please try again.";
            return View(model);
        }
    }

    #endregion
}