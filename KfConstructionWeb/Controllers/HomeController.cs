using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.ViewModels;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Controllers;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEmailService _emailService;

    public HomeController(ILogger<HomeController> logger, ISiteConfigService siteConfigService, IEmailService emailService) 
        : base(siteConfigService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Services()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var success = await _emailService.SendContactFormAsync(
                model.Name, 
                model.Email, 
                model.Subject, 
                model.Message);

            if (success)
            {
                TempData["SuccessMessage"] = "Thank you for your message! We'll get back to you soon.";
                return RedirectToAction(nameof(Contact));
            }
            else
            {
                TempData["ErrorMessage"] = "Sorry, there was an issue sending your message. Please try again or contact us directly.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing contact form submission");
            TempData["ErrorMessage"] = "Sorry, there was an issue sending your message. Please try again or contact us directly.";
        }

        return View(model);
    }

    public IActionResult Quote()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quote(QuoteRequestModel model)
    {
        // Check honeypot field for spam
        if (!string.IsNullOrEmpty(model.HoneyPot))
        {
            // Silently reject spam
            TempData["SuccessMessage"] = "Thank you for your quote request! We'll get back to you soon.";
            return RedirectToAction(nameof(Quote));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Format the quote request email
            var emailBody = $@"
New Quote Request Received

Contact Information:
-------------------
Name: {model.FullName}
Email: {model.Email}
Phone: {model.PhoneNumber}
Company: {model.Company ?? "N/A"}

Project Information:
-------------------
Services Needed: {model.ServicesNeeded}
Budget Range: {model.Budget ?? "Not specified"}

Project Details:
-------------------
{model.ProjectDetails}

---
This quote request was submitted on {DateTime.Now:MMMM dd, yyyy 'at' h:mm tt}
";

            var success = await _emailService.SendContactFormAsync(
                model.FullName,
                model.Email,
                $"Quote Request - {model.ServicesNeeded}",
                emailBody);

            if (success)
            {
                TempData["SuccessMessage"] = "Thank you for your quote request! We'll review your project details and get back to you within 24 hours.";
                return RedirectToAction(nameof(Quote));
            }
            else
            {
                TempData["ErrorMessage"] = "Sorry, there was an issue sending your quote request. Please try again or contact us directly.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing quote request submission");
            TempData["ErrorMessage"] = "Sorry, there was an issue sending your quote request. Please try again or contact us directly.";
        }

        return View(model);
    }

    public IActionResult AccessDenied(string? reason)
    {
        ViewBag.Reason = reason;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
