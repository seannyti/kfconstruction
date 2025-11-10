using KfConstructionAPI.Models;

namespace KfConstructionAPI.Services.Interfaces;

public interface IApiKeyService
{
    Task<ApiKey> CreateAsync(string name, string? description, string? createdBy, DateTime? expiresAt = null);
    Task<bool> RevokeAsync(int id, string revokedBy);
    Task<bool> DeleteAsync(int id);
    Task<IReadOnlyList<ApiKey>> GetAllAsync();
    Task<ApiKey?> GetByIdAsync(int id);
    Task<bool> ValidateAsync(string providedKey);
    Task RecordUsageAsync(int id);
}
