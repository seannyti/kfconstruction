using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using KfConstructionWeb.Data;

namespace KfConstructionWeb.Services.HealthChecks;

/// <summary>
/// Health check for database connectivity and performance
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(ApplicationDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Test database connectivity with a simple query
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            
            stopwatch.Stop();
            
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            var latency = stopwatch.ElapsedMilliseconds;

            // Check if latency is acceptable
            if (latency > 1000) // > 1 second is degraded
            {
                return HealthCheckResult.Degraded(
                    $"Database responding slowly: {latency}ms",
                    data: new Dictionary<string, object>
                    {
                        { "latency_ms", latency }
                    });
            }

            return HealthCheckResult.Healthy(
                $"Database is healthy (latency: {latency}ms)",
                data: new Dictionary<string, object>
                {
                    { "latency_ms", latency }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                exception: ex);
        }
    }
}

/// <summary>
/// Health check for Receipt OCR service availability
/// </summary>
public class OcrServiceHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcrServiceHealthCheck> _logger;

    public OcrServiceHealthCheck(IConfiguration configuration, ILogger<OcrServiceHealthCheck> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _configuration["ReceiptSettings:AzureFormRecognizerEndpoint"];
            var apiKey = _configuration["ReceiptSettings:AzureFormRecognizerApiKey"];

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "OCR service not configured (manual entry still works)",
                    data: new Dictionary<string, object>
                    {
                        { "configured", false }
                    }));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "OCR service is configured",
                data: new Dictionary<string, object>
                {
                    { "configured", true },
                    { "endpoint", endpoint }
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR service health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "OCR service health check failed",
                exception: ex));
        }
    }
}

/// <summary>
/// Health check for file storage availability
/// </summary>
public class FileStorageHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageHealthCheck> _logger;

    public FileStorageHealthCheck(IWebHostEnvironment environment, ILogger<FileStorageHealthCheck> logger)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "receipts");

            // Check if directory exists
            if (!Directory.Exists(uploadPath))
            {
                // Try to create it
                Directory.CreateDirectory(uploadPath);
            }

            // Test write permissions
            var testFile = Path.Combine(uploadPath, $".health_check_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "health check");
            File.Delete(testFile);

            return Task.FromResult(HealthCheckResult.Healthy(
                "File storage is accessible",
                data: new Dictionary<string, object>
                {
                    { "path", uploadPath },
                    { "writable", true }
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File storage health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "File storage is not accessible",
                exception: ex));
        }
    }
}
