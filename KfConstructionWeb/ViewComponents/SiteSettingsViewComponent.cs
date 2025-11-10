using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.ViewComponents;

public class SiteSettingsViewComponent : ViewComponent
{
    private readonly ISettingsService _settingsService;

    public SiteSettingsViewComponent(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = new SiteSettingsViewModel
        {
            CompanyName = await _settingsService.GetSettingAsync("General.CompanyName", "KF Construction") ?? "KF Construction",
            SiteTitle = await _settingsService.GetSettingAsync("General.SiteTitle", "KF Construction Management") ?? "KF Construction Management"
        };

        return View(settings);
    }
}

public class SiteSettingsViewModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string SiteTitle { get; set; } = string.Empty;
}