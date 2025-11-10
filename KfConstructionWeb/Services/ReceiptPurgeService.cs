using Microsoft.EntityFrameworkCore;
using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

/// <summary>
/// Background service for automatic purging of expired receipts based on retention policy
/// GDPR/CCPA compliant data retention implementation
/// </summary>
public class ReceiptPurgeService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReceiptPurgeService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval;
    private readonly IScheduledTaskRegistry? _taskRegistry;

    public ReceiptPurgeService(
        IServiceProvider serviceProvider,
        ILogger<ReceiptPurgeService> logger,
        IConfiguration configuration,
        IScheduledTaskRegistry? taskRegistry = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _taskRegistry = taskRegistry; // optional for tests

        // Run daily at 2 AM by default (configurable)
        _checkInterval = TimeSpan.FromHours(24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Receipt Purge Service started");

        // Wait until 2 AM on first run
        var initialDelay = await WaitUntilScheduledTime(stoppingToken);
        if (_taskRegistry != null)
        {
            _taskRegistry.ReportScheduled("ReceiptPurge", "Daily purge of expired receipts", DateTime.UtcNow.Add(initialDelay));
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime loopStart = DateTime.UtcNow;
            if (_taskRegistry != null)
            {
                _taskRegistry.ReportStart("ReceiptPurge", "Daily purge of expired receipts", DateTime.UtcNow.Add(_checkInterval));
            }
            try
            {
                await PurgeExpiredReceiptsAsync(stoppingToken);
                var duration = DateTime.UtcNow - loopStart;
                _taskRegistry?.ReportSuccess("ReceiptPurge", duration, "Completed purge cycle", DateTime.UtcNow.Add(_checkInterval));
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - loopStart;
                _taskRegistry?.ReportFailure("ReceiptPurge", duration, ex.Message, DateTime.UtcNow.Add(_checkInterval));
                _logger.LogError(ex, "Error occurred while purging receipts");
            }

            // Wait until next scheduled time
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task<TimeSpan> WaitUntilScheduledTime(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0); // 2 AM

        if (now > scheduledTime)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        var delay = scheduledTime - now;
        _logger.LogInformation("Receipt purge service will run at {ScheduledTime} (in {Delay})", 
            scheduledTime, delay);

        if (delay.TotalMilliseconds > 0)
        {
            await Task.Delay(delay, cancellationToken);
        }
        return delay;
    }

    private async Task PurgeExpiredReceiptsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IFileEncryptionService>();

        var now = DateTime.UtcNow;

        // Find receipts that are past their purge date
        var expiredReceipts = await context.Receipts
            .Where(r => r.IsDeleted && r.ScheduledPurgeDate.HasValue && r.ScheduledPurgeDate.Value <= now)
            .ToListAsync(cancellationToken);

        if (expiredReceipts.Count == 0)
        {
            _logger.LogInformation("No expired receipts to purge");
            return;
        }

        _logger.LogInformation("Found {Count} expired receipts to purge", expiredReceipts.Count);

        var purgedCount = 0;
        var errorCount = 0;

        foreach (var receipt in expiredReceipts)
        {
            try
            {
                // Delete encrypted file from disk
                if (!string.IsNullOrWhiteSpace(receipt.EncryptedFilePath))
                {
                    await encryptionService.SecureDeleteFileAsync(receipt.EncryptedFilePath);
                    _logger.LogInformation("Securely deleted file for receipt ID={ReceiptId}", receipt.Id);
                }

                // Delete access logs
                var accessLogs = await context.ReceiptAccessLogs
                    .Where(l => l.ReceiptId == receipt.Id)
                    .ToListAsync(cancellationToken);

                context.ReceiptAccessLogs.RemoveRange(accessLogs);

                // Delete receipt record
                context.Receipts.Remove(receipt);

                await context.SaveChangesAsync(cancellationToken);

                purgedCount++;
                _logger.LogInformation(
                    "Purged receipt: ID={ReceiptId}, Vendor={Vendor}, Amount={Amount}, DeletedAt={DeletedAt}",
                    receipt.Id, receipt.Vendor, receipt.TotalAmount, receipt.DeletedAt);
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Error purging receipt ID={ReceiptId}", receipt.Id);
            }
        }

        _logger.LogInformation(
            "Receipt purge completed: {PurgedCount} purged, {ErrorCount} errors",
            purgedCount, errorCount);

        // Log purge audit trail
        await LogPurgeAuditAsync(context, purgedCount, errorCount, cancellationToken);
    }

    private Task LogPurgeAuditAsync(
        ApplicationDbContext context,
        int purgedCount,
        int errorCount,
        CancellationToken cancellationToken)
    {
        try
        {
            // You could create a separate audit table for this if needed
            _logger.LogInformation(
                "Purge audit: Date={Date}, Purged={Purged}, Errors={Errors}",
                DateTime.UtcNow, purgedCount, errorCount);

            // Optional: Save to database audit table
            // var audit = new PurgeAuditLog
            // {
            //     PurgeDate = DateTime.UtcNow,
            //     PurgedCount = purgedCount,
            //     ErrorCount = errorCount
            // };
            // context.PurgeAuditLogs.Add(audit);
            // await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging purge audit");
        }
        
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receipt Purge Service stopping");
        await base.StopAsync(cancellationToken);
    }
}
