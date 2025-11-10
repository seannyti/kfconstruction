using System.ComponentModel.DataAnnotations;

namespace KfConstructionWeb.Models.ViewModels;

public class SettingsViewModel
{
    public GeneralSettingsViewModel General { get; set; } = new();
    public SecuritySettingsViewModel Security { get; set; } = new();
    public EmailSettingsViewModel Email { get; set; } = new();
    public MaintenanceSettingsViewModel Maintenance { get; set; } = new();
}

public class GeneralSettingsViewModel
{
    [Display(Name = "Company Name")]
    [Required]
    [MaxLength(100)]
    public string CompanyName { get; set; } = string.Empty;
    
    [Display(Name = "Company Email")]
    [Required]
    [EmailAddress]
    public string CompanyEmail { get; set; } = string.Empty;
    
    [Display(Name = "Company Phone")]
    [MaxLength(20)]
    public string? CompanyPhone { get; set; }
    
    [Display(Name = "Company Address")]
    [MaxLength(200)]
    public string? CompanyAddress { get; set; }
    
    [Display(Name = "Site Title")]
    [Required]
    [MaxLength(100)]
    public string SiteTitle { get; set; } = string.Empty;
    
    [Display(Name = "Site Description")]
    [MaxLength(500)]
    public string? SiteDescription { get; set; }
}

public class SecuritySettingsViewModel
{
    [Display(Name = "Require Email Confirmation")]
    public bool RequireEmailConfirmation { get; set; }
    
    [Display(Name = "Enable Two-Factor Authentication")]
    public bool EnableTwoFactorAuth { get; set; }
    
    [Display(Name = "Session Timeout (minutes)")]
    [Range(5, 1440)]
    public int SessionTimeoutMinutes { get; set; } = 30;
    
    [Display(Name = "Password Minimum Length")]
    [Range(6, 20)]
    public int PasswordMinLength { get; set; } = 8;
    
    [Display(Name = "Require Password Special Characters")]
    public bool RequirePasswordSpecialChars { get; set; } = true;
    
    [Display(Name = "Maximum Login Attempts")]
    [Range(3, 10)]
    public int MaxLoginAttempts { get; set; } = 5;
}

public class EmailSettingsViewModel
{
    [Display(Name = "Enable Emails")]
    public bool EnableEmails { get; set; } = true;
    
    [Display(Name = "SMTP Server")]
    [MaxLength(100)]
    public string? SmtpServer { get; set; }
    
    [Display(Name = "SMTP Port")]
    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;
    
    [Display(Name = "SMTP Username")]
    [MaxLength(100)]
    public string? SmtpUsername { get; set; }
    
    [Display(Name = "SMTP Password")]
    [DataType(DataType.Password)]
    [MaxLength(100)]
    public string? SmtpPassword { get; set; }
    
    [Display(Name = "Use SSL")]
    public bool UseSSL { get; set; } = true;
    
    [Display(Name = "From Email Address")]
    [EmailAddress]
    [MaxLength(100)]
    public string? FromEmail { get; set; }
    
    [Display(Name = "From Display Name")]
    [MaxLength(100)]
    public string? FromDisplayName { get; set; }
}

public class MaintenanceSettingsViewModel
{
    [Display(Name = "Maintenance Mode")]
    public bool MaintenanceMode { get; set; }
    
    [Display(Name = "Maintenance Message")]
    [MaxLength(500)]
    public string? MaintenanceMessage { get; set; }
    
    [Display(Name = "Enable User Registration")]
    public bool EnableUserRegistration { get; set; } = true;
    
    [Display(Name = "Enable API Access")]
    public bool EnableApiAccess { get; set; } = true;
}