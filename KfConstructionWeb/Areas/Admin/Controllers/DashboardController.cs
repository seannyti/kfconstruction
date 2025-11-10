using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Controllers;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : BaseController
{
    public DashboardController(ISiteConfigService siteConfigService) : base(siteConfigService)
    {
    }

    public IActionResult Index()
    {
        return View();
    }
}
