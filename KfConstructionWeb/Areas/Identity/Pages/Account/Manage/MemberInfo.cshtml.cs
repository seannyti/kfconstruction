using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.DTOs;

namespace KfConstructionWeb.Areas.Identity.Pages.Account.Manage;

[Authorize(Roles = "Admin,SuperAdmin")]
public class MemberInfoModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;

    public MemberInfoModel(UserManager<IdentityUser> userManager, IHttpClientFactory httpClientFactory)
    {
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
    }

    public MemberDto? Member { get; set; }
    public string StatusMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadMemberDataAsync(user);
        return Page();
    }

    private async Task LoadMemberDataAsync(IdentityUser user)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("KfConstructionAPI");
            var response = await client.GetAsync($"/api/v1/Members/by-email/{user.Email}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<APIResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.Result != null)
                {
                    var memberJson = JsonSerializer.Serialize(apiResponse.Result);
                    Member = JsonSerializer.Deserialize<MemberDto>(memberJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            else
            {
                StatusMessage = "No member record found in the system.";
            }
        }
        catch (Exception)
        {
            StatusMessage = "Error loading member information.";
        }
    }
}