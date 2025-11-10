using KfConstructionWeb.Data;
using KfConstructionWeb.Services;
using KfConstructionWeb.Services.Interfaces;
using KfConstructionWeb.Models.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        // Account settings
        options.SignIn.RequireConfirmedAccount = false;
        
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8; // Increased from 6
        options.Password.RequireNonAlphanumeric = true; // Changed to true
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredUniqueChars = 4; // Added
        
        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        
        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddHttpClient("KfConstructionAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("X-API-Key", builder.Configuration["ApiSettings:ApiKey"]);
});

// Register EmailSender for Identity functionality
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Configure Email Settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Register Email Service for business logic
builder.Services.AddScoped<IEmailService, EmailSender>();

// Configure Security Settings
builder.Services.Configure<SecurityConfiguration>(
    builder.Configuration.GetSection("Security"));

// Register User Deletion Service
builder.Services.AddScoped<IUserDeletionService, UserDeletionService>();

// Register Settings Service
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Register Site Config Service
builder.Services.AddScoped<ISiteConfigService, SiteConfigService>();

// Register Account Lock Service
builder.Services.AddScoped<IAccountLockService, AccountLockService>();

// Register Testimonial Service
builder.Services.AddScoped<ITestimonialService, TestimonialService>();

// Register Testimonial Rate Limiting Service
builder.Services.AddScoped<ITestimonialRateLimitService, TestimonialRateLimitService>();

// Register Receipt Services
builder.Services.AddScoped<IReceiptOcrService, ReceiptOcrService>();
builder.Services.AddSingleton<IFileEncryptionService, FileEncryptionService>();
builder.Services.AddSingleton<IReceiptRateLimitService, ReceiptRateLimitService>();

// Register File Management Service
builder.Services.AddScoped<IFileManagementService, FileManagementService>();

// Register Activity Log Service
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

// Register Backup/Restore Service
builder.Services.AddScoped<IBackupRestoreService, BackupRestoreService>();

// Register Broadcast Message Service
builder.Services.AddScoped<IBroadcastMessageService, BroadcastMessageService>();

// Register Scheduled Task Registry (for dashboard)
builder.Services.AddSingleton<IScheduledTaskRegistry, ScheduledTaskRegistry>();

// Register Receipt Purge Background Service (runs daily at 2 AM)
builder.Services.AddHostedService<ReceiptPurgeService>();

// Register Performance Tracking
builder.Services.AddSingleton<IPerformanceTracker, PerformanceTracker>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<KfConstructionWeb.Services.HealthChecks.DatabaseHealthCheck>("database")
    .AddCheck<KfConstructionWeb.Services.HealthChecks.OcrServiceHealthCheck>("ocr_service")
    .AddCheck<KfConstructionWeb.Services.HealthChecks.FileStorageHealthCheck>("file_storage");

// Configure Receipt Settings
builder.Services.Configure<ReceiptSettings>(
    builder.Configuration.GetSection("ReceiptSettings"));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        
    // Control user clearing via environment variable CLEAR_USERS ("1"/"true")
    var clearUsersEnv = Environment.GetEnvironmentVariable("CLEAR_USERS");
    bool clearExistingUsers = !string.IsNullOrWhiteSpace(clearUsersEnv) &&
                   (clearUsersEnv.Equals("1") || clearUsersEnv.Equals("true", StringComparison.OrdinalIgnoreCase));
        await SeedData.SeedRolesAndAdminAsync(services, clearExistingUsers);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Security Headers (OWASP ASVS L2 compliance)
app.Use(async (context, next) =>
{
    // Prevent MIME type sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    
    // Prevent clickjacking attacks
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    
    // XSS protection (legacy browsers)
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    
    // Control referrer information
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Restrict feature permissions
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    
    // Content Security Policy (CSP) - Critical XSS protection
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self'; " +
        "frame-ancestors 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
    
    await next();
});

app.UseHttpsRedirection();
app.UseRouting();

// Add performance tracking middleware
app.UseMiddleware<KfConstructionWeb.Middleware.PerformanceTrackingMiddleware>();

app.UseAuthentication();

// Add account lock middleware after authentication but before authorization
app.UseMiddleware<KfConstructionWeb.Middleware.AccountLockMiddleware>();

// Add maintenance mode middleware after authentication
app.UseMiddleware<KfConstructionWeb.Middleware.MaintenanceModeMiddleware>();

app.UseAuthorization();

app.UseStaticFiles();
app.MapRazorPages();

// Map health check endpoints
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
