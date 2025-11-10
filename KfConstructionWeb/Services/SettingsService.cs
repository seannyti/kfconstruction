using KfConstructionWeb.Data;
using KfConstructionWeb.Models;
using KfConstructionWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KfConstructionWeb.Services;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ApplicationDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        try
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == key);
            
            return setting?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting with key: {Key}", key);
            return null;
        }
    }

    public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
    {
        try
        {
            var value = await GetSettingAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            // Handle different types
            if (typeof(T) == typeof(string))
                return (T)(object)value;
            
            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(value);
            
            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(value);
            
            if (typeof(T) == typeof(DateTime))
                return (T)(object)DateTime.Parse(value);

            // For complex types, try JSON deserialization
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing setting {Key} to type {Type}", key, typeof(T).Name);
            return defaultValue;
        }
    }

    public async Task SetSettingAsync(string key, string value, string category = "General", string? description = null, string? modifiedBy = null)
    {
        try
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new AppSetting
                {
                    Key = key,
                    Value = value,
                    Category = category,
                    Description = description,
                    ModifiedBy = modifiedBy,
                    LastModified = DateTime.UtcNow
                };
                _context.AppSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.Category = category;
                setting.Description = description ?? setting.Description;
                setting.ModifiedBy = modifiedBy;
                setting.LastModified = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key: {Key}", key);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category)
    {
        try
        {
            var settings = await _context.AppSettings
                .Where(s => s.Category == category)
                .ToDictionaryAsync(s => s.Key, s => s.Value);
            
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings for category: {Category}", category);
            return new Dictionary<string, string>();
        }
    }

    public async Task<Dictionary<string, AppSetting>> GetAllSettingsAsync()
    {
        try
        {
            var settings = await _context.AppSettings
                .ToDictionaryAsync(s => s.Key, s => s);
            
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all settings");
            return new Dictionary<string, AppSetting>();
        }
    }

    public async Task DeleteSettingAsync(string key)
    {
        try
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == key);
            
            if (setting != null)
            {
                _context.AppSettings.Remove(setting);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setting with key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> SettingExistsAsync(string key)
    {
        try
        {
            return await _context.AppSettings
                .AnyAsync(s => s.Key == key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if setting exists: {Key}", key);
            return false;
        }
    }
}