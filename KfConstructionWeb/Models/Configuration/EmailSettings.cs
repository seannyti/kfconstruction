namespace KfConstructionWeb.Models.Configuration;

/// <summary>
/// Email service configuration settings
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Email service provider (SendGrid, Mailgun, SMTP)
    /// </summary>
    public string Provider { get; set; } = "SMTP";
    
    /// <summary>
    /// SMTP server settings
    /// </summary>
    public SmtpSettings Smtp { get; set; } = new();
    
    /// <summary>
    /// SendGrid API key (if using SendGrid)
    /// </summary>
    public string? SendGridApiKey { get; set; }
    
    /// <summary>
    /// Default sender information
    /// </summary>
    public EmailSenderInfo DefaultSender { get; set; } = new();
    
    /// <summary>
    /// Admin notification settings
    /// </summary>
    public EmailSenderInfo AdminNotifications { get; set; } = new();
    
    /// <summary>
    /// Enable/disable email functionality
    /// </summary>
    public bool EnableEmails { get; set; } = true;
    
    /// <summary>
    /// Development mode (logs emails instead of sending)
    /// </summary>
    public bool DevelopmentMode { get; set; } = false;
}

public class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class EmailSenderInfo
{
    public string Name { get; set; } = "KF Construction";
    public string Email { get; set; } = "knudsonfamilyconstruction@yahoo.com";
}