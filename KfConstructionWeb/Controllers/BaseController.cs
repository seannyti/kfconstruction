using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Controllers;

public class BaseController : Controller
{
    protected readonly ISiteConfigService _siteConfigService;

    public BaseController(ISiteConfigService siteConfigService)
    {
        _siteConfigService = siteConfigService;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Set site-wide ViewData before any action executes
        ViewData["CompanyName"] = await _siteConfigService.GetCompanyNameAsync();
        ViewData["CompanyEmail"] = await _siteConfigService.GetCompanyEmailAsync();
        ViewData["CompanyPhone"] = await _siteConfigService.GetCompanyPhoneAsync();
        ViewData["CompanyAddress"] = await _siteConfigService.GetCompanyAddressAsync();
        ViewData["SiteTitle"] = await _siteConfigService.GetSiteTitleAsync();
        ViewData["SiteDescription"] = await _siteConfigService.GetSiteDescriptionAsync();

        await next();
    }
}