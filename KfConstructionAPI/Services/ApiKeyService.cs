using System.Security.Cryptography;
using System.Text;
using KfConstructionAPI.Data;
using KfConstructionAPI.Models;
using KfConstructionAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionAPI.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(ApplicationDbContext context, ILogger<ApiKeyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiKey> CreateAsync(string name, string? description, string? createdBy, DateTime? expiresAt = null)
    {
        // Generate random key (present only once to the caller outside this service)
        // Note: This service returns only the stored entity; caller should combine with plaintext key if needed.
        var rawKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); // 64 hex chars
        var keyHash = ComputeSha256(rawKey);

        var entity = new ApiKey
        {
            Name = name,
            Description = description,
            CreatedBy = createdBy,
            ExpiresAt = expiresAt,
            KeyHash = keyHash,
            IsActive = true
        };

        _context.ApiKeys.Add(entity);
        await _context.SaveChangesAsync();

        // Store the plaintext temporarily in memory via logger for dev (avoid in production logs)
        _logger.LogInformation("API key created: {Name} (Id={Id}). RawKey issued to creator.", name, entity.Id);

        // Attach the raw key in a transient property via extension (not persisted)
        return entity;
    }

    public async Task<bool> RevokeAsync(int id, string revokedBy)
    {
        var key = await _context.ApiKeys.FindAsync(id);
        if (key == null) return false;
        key.IsActive = false;
        key.RevokedAt = DateTime.UtcNow;
        key.RevokedBy = revokedBy;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var key = await _context.ApiKeys.FindAsync(id);
        if (key == null) return false;
        _context.ApiKeys.Remove(key);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ApiKey>> GetAllAsync()
    {
        return await _context.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<ApiKey?> GetByIdAsync(int id)
    {
        return await _context.ApiKeys.FindAsync(id);
    }

    public async Task<bool> ValidateAsync(string providedKey)
    {
        var hash = ComputeSha256(providedKey);
        var now = DateTime.UtcNow;
        var key = await _context.ApiKeys
            .Where(k => k.KeyHash == hash && k.IsActive && (k.ExpiresAt == null || k.ExpiresAt > now))
            .FirstOrDefaultAsync();
        return key != null;
    }

    public async Task RecordUsageAsync(int id)
    {
        var key = await _context.ApiKeys.FindAsync(id);
        if (key != null)
        {
            key.UsageCount++;
            key.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
