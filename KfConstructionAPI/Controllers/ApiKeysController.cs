using System.Security.Cryptography;
using System.Text;
using KfConstructionAPI.Data;
using KfConstructionAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiKeysController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(ApplicationDbContext context, ILogger<ApiKeysController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var keys = await _context.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new
            {
                k.Id,
                k.Name,
                k.Description,
                k.CreatedAt,
                k.CreatedBy,
                k.ExpiresAt,
                k.LastUsedAt,
                k.UsageCount,
                k.IsActive,
                k.RevokedAt,
                k.RevokedBy
            })
            .ToListAsync();
        return Ok(keys);
    }

    public record CreateKeyRequest(string Name, string? Description, DateTime? ExpiresAt, string? CreatedBy);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Name is required");

        var rawKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var keyHash = ComputeSha256(rawKey);

        var entity = new ApiKey
        {
            Name = request.Name,
            Description = request.Description,
            CreatedBy = request.CreatedBy,
            ExpiresAt = request.ExpiresAt,
            KeyHash = keyHash,
            IsActive = true
        };

        _context.ApiKeys.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new { entity.Id, entity.Name, entity.Description, entity.CreatedAt, entity.ExpiresAt, entity.IsActive, ApiKey = rawKey });
    }

    [HttpPost("revoke/{id:int}")]
    public async Task<IActionResult> Revoke(int id)
    {
        var key = await _context.ApiKeys.FindAsync(id);
        if (key == null) return NotFound();
        key.IsActive = false;
        key.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var key = await _context.ApiKeys.FindAsync(id);
        if (key == null) return NotFound();
        _context.ApiKeys.Remove(key);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
