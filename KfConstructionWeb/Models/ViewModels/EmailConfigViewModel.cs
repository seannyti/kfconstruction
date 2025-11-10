namespace KfConstructionWeb.Models.ViewModels;

public class EmailConfigViewModel
{
    // Effective values resolved from DB settings or appsettings fallback
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; } // For input only; do not echo back
    public bool UseSsl { get; set; } = true;
    public bool EnableEmails { get; set; } = true;
    public bool DevelopmentMode { get; set; } = false;

    // Display-only hints
    public string DefaultSenderEmail { get; set; } = string.Empty;
    public string DefaultSenderName { get; set; } = string.Empty;

    // Test email
    public string? TestRecipient { get; set; }
    public string? LastResult { get; set; }
}
