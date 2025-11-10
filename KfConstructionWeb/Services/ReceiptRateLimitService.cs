using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using KfConstructionWeb.Models.Configuration;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

/// <summary>
/// Rate limit result for receipt submissions
/// </summary>
public class ReceiptRateLimitResult
{
    public bool IsAllowed { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AttemptsInWindow { get; set; }
    public int MaxAllowed { get; set; }
    public TimeSpan WindowDuration { get; set; }
    public DateTime? NextAllowedTime { get; set; }
}

/// <summary>
/// Implementation of receipt upload rate limiting
/// Uses in-memory cache with sliding expiration
/// </summary>
public class ReceiptRateLimitService : IReceiptRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReceiptRateLimitService> _logger;
    private readonly int _maxUploadsPerWindow;
    private readonly TimeSpan _rateLimitWindow;

    public ReceiptRateLimitService(
        IMemoryCache cache,
        ILogger<ReceiptRateLimitService> logger,
        IConfiguration configuration)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Load configuration with defaults
        // Receipts are more resource-intensive than testimonials, so lower limits
        _maxUploadsPerWindow = configuration.GetValue<int>("ReceiptSettings:MaxUploadsPerHour", 10);
        var windowHours = configuration.GetValue<int>("ReceiptSettings:RateLimitWindowHours", 1);
        _rateLimitWindow = TimeSpan.FromHours(windowHours);

        _logger.LogInformation(
            "Receipt rate limiting initialized: {MaxUploads} uploads per {WindowHours} hour(s)",
            _maxUploadsPerWindow, windowHours);
    }

    /// <inheritdoc />
    public Task<ReceiptRateLimitResult> CheckRateLimitAsync(string ipAddress)
    {
        var result = new ReceiptRateLimitResult
        {
            MaxAllowed = _maxUploadsPerWindow,
            WindowDuration = _rateLimitWindow
        };

        // Validate IP address
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _logger.LogWarning("Rate limit check called with empty IP address");
            result.IsAllowed = false;
            result.Message = "Invalid request - IP address required";
            return Task.FromResult(result);
        }

        var cacheKey = GetCacheKey(ipAddress);

        // Check existing uploads
        if (_cache.TryGetValue(cacheKey, out List<DateTime>? uploads) && uploads != null)
        {
            // Remove expired entries outside the window
            var cutoff = DateTime.UtcNow.Subtract(_rateLimitWindow);
            uploads.RemoveAll(u => u < cutoff);

            result.AttemptsInWindow = uploads.Count;

            // Check if limit exceeded
            if (uploads.Count >= _maxUploadsPerWindow)
            {
                result.IsAllowed = false;
                result.NextAllowedTime = uploads.Min().Add(_rateLimitWindow);
                
                result.Message = $"Upload limit exceeded. Maximum {_maxUploadsPerWindow} uploads per " +
                                $"{_rateLimitWindow.TotalHours:F0} hour(s). Next upload allowed at " +
                                $"{result.NextAllowedTime:yyyy-MM-dd HH:mm:ss} UTC.";

                _logger.LogWarning(
                    "Rate limit exceeded for IP {IpAddress}: {Count}/{Max} uploads in window",
                    ipAddress, uploads.Count, _maxUploadsPerWindow);

                return Task.FromResult(result);
            }

            // Update cache with cleaned list
            _cache.Set(cacheKey, uploads, new MemoryCacheEntryOptions
            {
                SlidingExpiration = _rateLimitWindow
            });
        }

        result.IsAllowed = true;
        result.Message = $"Upload allowed ({result.AttemptsInWindow}/{_maxUploadsPerWindow})";
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task RecordUploadAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _logger.LogWarning("Attempted to record upload with empty IP address");
            return;
        }

        var cacheKey = GetCacheKey(ipAddress);

        // Get or create upload list
        var uploads = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = _rateLimitWindow;
            return new List<DateTime>();
        }) ?? new List<DateTime>();

        // Add current upload
        uploads.Add(DateTime.UtcNow);

        // Update cache
        _cache.Set(cacheKey, uploads, new MemoryCacheEntryOptions
        {
            SlidingExpiration = _rateLimitWindow
        });

        _logger.LogInformation(
            "Recorded receipt upload for IP {IpAddress}: {Count}/{Max} in current window",
            ipAddress, uploads.Count, _maxUploadsPerWindow);

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> GetUploadCountAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return 0;
        }

        var cacheKey = GetCacheKey(ipAddress);

        if (_cache.TryGetValue(cacheKey, out List<DateTime>? uploads) && uploads != null)
        {
            // Remove expired entries
            var cutoff = DateTime.UtcNow.Subtract(_rateLimitWindow);
            uploads.RemoveAll(u => u < cutoff);
            
            return uploads.Count;
        }

        await Task.CompletedTask;
        return 0;
    }

    /// <summary>
    /// Generates cache key for IP address
    /// </summary>
    private static string GetCacheKey(string ipAddress)
    {
        return $"receipt_rate_limit_{ipAddress}";
    }
}
