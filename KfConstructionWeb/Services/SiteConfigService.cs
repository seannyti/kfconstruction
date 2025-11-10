using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

public class SiteConfigService : ISiteConfigService
{
    private readonly ISettingsService _settingsService;

    // Default values for site configuration
    private static readonly Dictionary<string, string> DefaultValues = new()
    {
        { "General.CompanyName", "KF Construction" },
        { "General.CompanyEmail", "knudsonfamilyconstruction@yahoo.com" },
        { "General.CompanyPhone", "" },
        { "General.CompanyAddress", "" },
        { "General.SiteTitle", "KF Construction Management" },
        { "General.SiteDescription", "Professional construction services" }
    };

    public SiteConfigService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    private async Task<string> GetSettingWithDefaultAsync(string key)
    {
        var defaultValue = DefaultValues.GetValueOrDefault(key, "");
        return await _settingsService.GetSettingAsync(key, defaultValue) ?? defaultValue;
    }

    public async Task<string> GetCompanyNameAsync()
        => await GetSettingWithDefaultAsync("General.CompanyName");

    public async Task<string> GetCompanyEmailAsync()
        => await GetSettingWithDefaultAsync("General.CompanyEmail");

    public async Task<string> GetCompanyPhoneAsync()
        => await GetSettingWithDefaultAsync("General.CompanyPhone");

    public async Task<string> GetCompanyAddressAsync()
        => await GetSettingWithDefaultAsync("General.CompanyAddress");

    public async Task<string> GetSiteTitleAsync()
        => await GetSettingWithDefaultAsync("General.SiteTitle");

    public async Task<string> GetSiteDescriptionAsync()
        => await GetSettingWithDefaultAsync("General.SiteDescription");

    public async Task<string> GetPageTitleAsync(string pageTitle)
    {
        var companyName = await GetCompanyNameAsync();
        return $"{pageTitle} - {companyName}";
    }
}