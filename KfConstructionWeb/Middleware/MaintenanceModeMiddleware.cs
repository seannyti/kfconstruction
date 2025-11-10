using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Middleware;

/// <summary>
/// Middleware that handles maintenance mode functionality, blocking access to regular users while allowing administrators to continue managing the site
/// </summary>
public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceModeMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the MaintenanceModeMiddleware
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">The logger instance for tracking middleware operations</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
    public MaintenanceModeMiddleware(RequestDelegate next, ILogger<MaintenanceModeMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request and determines whether to allow access based on maintenance mode status and user roles
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <param name="settingsService">The settings service for retrieving maintenance mode configuration</param>
    public async Task InvokeAsync(HttpContext context, ISettingsService settingsService)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));

        try
        {
            // Check if maintenance mode is enabled
            var maintenanceMode = await settingsService.GetSettingAsync("Maintenance.MaintenanceMode", false);
            
            if (maintenanceMode)
            {
                _logger.LogInformation("Maintenance mode is active, checking user access for path: {Path}", context.Request.Path);

                // Allow admins to access the site during maintenance
                if (context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin"))
                {
                    _logger.LogInformation("Admin user accessing site during maintenance mode: {User}", context.User.Identity?.Name ?? "Unknown");
                    await _next(context);
                    return;
                }

                // Allow access to essential endpoints (auth, admin, static files)
                var allowedPaths = MiddlewareHelpers.CommonSkipPaths
                    .Concat(MiddlewareHelpers.AuthPaths)
                    .Concat(MiddlewareHelpers.AdminPaths)
                    .ToArray();

                if (MiddlewareHelpers.ShouldSkipPath(context.Request.Path.Value, allowedPaths))
                {
                    await _next(context);
                    return;
                }

                // Show maintenance page for all other users
                _logger.LogInformation("Showing maintenance page to non-admin user for path: {Path}", context.Request.Path);
                await ShowMaintenancePage(context, settingsService);
                return;
            }

            // Maintenance mode is disabled, continue normally
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in maintenance mode middleware for path: {Path}", context.Request.Path);
            // Continue to next middleware to avoid breaking the request pipeline
            await _next(context);
        }
    }

    /// <summary>
    /// Renders a professional maintenance page with administrator access option
    /// </summary>
    /// <param name="context">The HTTP context for rendering the response</param>
    /// <param name="settingsService">Service for retrieving maintenance and company settings</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task ShowMaintenancePage(HttpContext context, ISettingsService settingsService)
    {
        try
        {
            // Retrieve customizable maintenance settings
            var message = await settingsService.GetSettingAsync("Maintenance.MaintenanceMessage", 
                "We're performing scheduled maintenance. Please check back later.");
            var companyName = await settingsService.GetSettingAsync("General.CompanyName", "KF Construction");

            context.Response.StatusCode = 503; // Service Unavailable
            context.Response.ContentType = "text/html";

            // Generate professional maintenance page with admin access option
            var html = GenerateMaintenancePageHtml(message ?? "Service temporarily unavailable", companyName ?? "KF Construction");
            await context.Response.WriteAsync(html);
            
            _logger.LogInformation("Maintenance page displayed successfully for path: {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying maintenance page for path: {Path}", context.Request.Path);
            
            // Fallback minimal maintenance response
            context.Response.StatusCode = 503;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Service temporarily unavailable. Please try again later.");
        }
    }

    /// <summary>
    /// Generates the HTML content for the maintenance page with professional styling and admin access
    /// </summary>
    /// <param name="message">The custom maintenance message to display</param>
    /// <param name="companyName">The company name for branding</param>
    /// <returns>Complete HTML content for the maintenance page</returns>
    private static string GenerateMaintenancePageHtml(string message, string companyName)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Maintenance Mode - {companyName}</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"">
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css"" rel=""stylesheet"">
    <style>
        body {{ 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', sans-serif;
        }}
        .maintenance-card {{
            background: rgba(255, 255, 255, 0.95);
            border-radius: 20px;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
            backdrop-filter: blur(10px);
        }}
        .maintenance-icon {{
            font-size: 4rem;
            color: #6c757d;
            animation: pulse 2s infinite;
        }}
        .admin-login-btn {{
            background: rgba(13, 110, 253, 0.1);
            border: 1px solid rgba(13, 110, 253, 0.2);
            color: #0d6efd;
            transition: all 0.3s ease;
        }}
        .admin-login-btn:hover {{
            background: rgba(13, 110, 253, 0.2);
            border-color: rgba(13, 110, 253, 0.4);
            color: #0a58ca;
            transform: translateY(-1px);
        }}
        @keyframes pulse {{
            0% {{ opacity: 1; }}
            50% {{ opacity: 0.5; }}
            100% {{ opacity: 1; }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""row justify-content-center"">
            <div class=""col-md-6 col-lg-5"">
                <div class=""card maintenance-card border-0"">
                    <div class=""card-body text-center p-5"">
                        <div class=""maintenance-icon mb-4"">
                            <i class=""bi bi-tools""></i>
                        </div>
                        <h1 class=""h3 mb-3 text-dark"">Maintenance Mode</h1>
                        <p class=""text-muted mb-4"">{message}</p>
                        <div class=""alert alert-info border-0"" style=""background: rgba(13, 110, 253, 0.1);"">
                            <i class=""bi bi-info-circle me-2""></i>
                            We appreciate your patience while we improve our services.
                        </div>
                        
                        <!-- Admin Login Option -->
                        <div class=""mt-4 mb-3"">
                            <a href=""/Identity/Account/Login?ReturnUrl=%2FAdmin%2FSettings"" 
                               class=""btn admin-login-btn px-4 py-2 rounded-pill"">
                                <i class=""bi bi-shield-lock me-2""></i>
                                Administrator Access
                            </a>
                        </div>
                        
                        <div class=""mt-4"">
                            <small class=""text-muted"">
                                <i class=""bi bi-clock me-1""></i>
                                Please try again in a few minutes
                            </small>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>";
    }
}