using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KfConstructionWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")] // Restrict API key management to SuperAdmin
public class ApiKeysController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IHttpClientFactory httpClientFactory, ILogger<ApiKeysController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateApiClient() => _httpClientFactory.CreateClient("KfConstructionAPI");

    public class ApiKeyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedBy { get; set; }
        public string? ApiKey { get; set; } // Only present on create
    }

    public class CreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var client = CreateApiClient();
            var keys = await client.GetFromJsonAsync<List<ApiKeyDto>>("api/ApiKeys");
            return View(keys ?? new());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load API keys");
            TempData["Error"] = "Failed to load API keys.";
            return View(new List<ApiKeyDto>());
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Name))
        {
            if (string.IsNullOrWhiteSpace(request.Name)) ModelState.AddModelError("Name", "Name is required.");
            return View(request);
        }

        try
        {
            var client = CreateApiClient();
            var payload = new
            {
                request.Name,
                request.Description,
                request.ExpiresAt,
                CreatedBy = User.Identity?.Name
            };
            var response = await client.PostAsJsonAsync("api/ApiKeys", payload);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
            TempData["Success"] = "API key created successfully. Copy it now—this is the only time it's shown.";
            ViewBag.PlaintextKey = created?.ApiKey;
            return View("Created", created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API key");
            TempData["Error"] = "Failed to create API key.";
            return View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(int id)
    {
        try
        {
            var client = CreateApiClient();
            var response = await client.PostAsync($"api/ApiKeys/revoke/{id}", null);
            response.EnsureSuccessStatusCode();
            TempData["Success"] = "API key revoked.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke API key");
            TempData["Error"] = "Failed to revoke API key.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var client = CreateApiClient();
            var response = await client.DeleteAsync($"api/ApiKeys/{id}");
            response.EnsureSuccessStatusCode();
            TempData["Success"] = "API key deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete API key");
            TempData["Error"] = "Failed to delete API key.";
        }
        return RedirectToAction(nameof(Index));
    }
}
