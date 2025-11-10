using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using KfConstructionWeb.Models;
using KfConstructionWeb.Models.DTOs;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

public class UserDeletionService : IUserDeletionService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserDeletionService> _logger;

    public UserDeletionService(
        UserManager<IdentityUser> userManager, 
        IHttpClientFactory httpClientFactory,
        ILogger<UserDeletionService> logger)
    {
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> DeleteUserCompletelyAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found for deletion");
                return false;
            }

            _logger.LogWarning($"Starting complete deletion for user {user.Email} ({userId})");

            // 1. Delete Member record from API first
            await DeleteMemberRecordAsync(userId);

            // 2. Delete Identity user account
            var result = await _userManager.DeleteAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogWarning($"Successfully deleted user {user.Email} and all associated data");
                return true;
            }
            else
            {
                _logger.LogError($"Failed to delete user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during complete user deletion for {userId}");
            return false;
        }
    }

    private async Task DeleteMemberRecordAsync(string userId)
    {
        try
        {
            var member = await GetMemberByUserIdAsync(userId);
            if (member != null)
            {
                var httpClient = _httpClientFactory.CreateClient("KfConstructionAPI");
                var response = await httpClient.DeleteAsync($"/api/v1/Members/{member.Id}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Deleted member record {member.Id} for user {userId}");
                }
                else
                {
                    _logger.LogWarning($"Failed to delete member record for user {userId}. Status: {response.StatusCode}");
                }
            }
            else
            {
                _logger.LogInformation($"No member record found for user {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting member record for user {userId}");
        }
    }

    public async Task<MemberDto?> GetMemberByUserIdAsync(string userId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("KfConstructionAPI");
            var response = await httpClient.GetAsync($"/api/v1/Members/user/{userId}");

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
                    return JsonSerializer.Deserialize<MemberDto>(memberJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving member for user {userId}");
        }

        return null;
    }
}