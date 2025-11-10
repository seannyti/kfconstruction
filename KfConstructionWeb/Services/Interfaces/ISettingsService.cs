using KfConstructionWeb.Models;

namespace KfConstructionWeb.Services.Interfaces;

public interface ISettingsService
{
    Task<string?> GetSettingAsync(string key);
    Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);
    Task SetSettingAsync(string key, string value, string category = "General", string? description = null, string? modifiedBy = null);
    Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category);
    Task<Dictionary<string, AppSetting>> GetAllSettingsAsync();
    Task DeleteSettingAsync(string key);
    Task<bool> SettingExistsAsync(string key);
}