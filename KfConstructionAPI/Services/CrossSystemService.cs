using KfConstructionAPI.Models.DTOs;
using KfConstructionAPI.Services.Interfaces;
using System.Text.Json;

namespace KfConstructionAPI.Services;

/// <summary>
/// Service for communicating with other systems using email as the linking identifier
/// </summary>
public class CrossSystemService : ICrossSystemService
{
    private readonly HttpClient _httpClient;
    private readonly IMemberService _memberService;
    private readonly ILogger<CrossSystemService> _logger;
    private readonly IConfiguration _configuration;

    public CrossSystemService(
        HttpClient httpClient,
        IMemberService memberService,
        ILogger<CrossSystemService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _memberService = memberService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets client information from the web application by email
    /// </summary>
    /// <param name="email">Email address to search for</param>
    /// <returns>Client information if found, null otherwise</returns>
    public async Task<WebClientDto?> GetWebClientByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("GetWebClientByEmailAsync called with null or empty email");
            return null;
        }

        try
        {
            // Get the web app base URL from configuration
            var webAppBaseUrl = _configuration["CrossSystem:WebAppBaseUrl"];
            if (string.IsNullOrEmpty(webAppBaseUrl))
            {
                _logger.LogWarning("WebAppBaseUrl not configured for cross-system communication");
                return null;
            }

            _logger.LogInformation("Querying web application for client with email: {Email}", email);

            // Make HTTP request to web app API
            var response = await _httpClient.GetAsync($"{webAppBaseUrl}/api/clients/by-email/{Uri.EscapeDataString(email)}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var client = JsonSerializer.Deserialize<WebClientDto>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Found client in web application for email: {Email}", email);
                return client;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No client found in web application for email: {Email}", email);
                return null;
            }
            else
            {
                _logger.LogWarning("Error querying web application for email {Email}: {StatusCode}", email, response.StatusCode);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while querying web application for email: {Email}", email);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for web client data with email: {Email}", email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while querying web application for email: {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Gets complete user profile across all systems by email
    /// </summary>
    /// <param name="email">Email address to search for</param>
    /// <returns>Aggregated user profile data</returns>
    public async Task<CrossSystemUserProfileDto> GetCrossSystemUserProfileAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("GetCrossSystemUserProfileAsync called with null or empty email");
            return new CrossSystemUserProfileDto { Email = email ?? string.Empty };
        }

        try
        {
            _logger.LogInformation("Building cross-system user profile for email: {Email}", email);

            // Get data from both systems concurrently for better performance
            var webClientTask = GetWebClientByEmailAsync(email);
            var apiMemberTask = GetApiMemberByEmailAsync(email);

            await Task.WhenAll(webClientTask, apiMemberTask);

            var profile = new CrossSystemUserProfileDto
            {
                Email = email,
                WebClient = webClientTask.Result,
                ApiMember = apiMemberTask.Result
            };

            _logger.LogInformation(
                "Cross-system profile built for {Email}: WebAccount={HasWeb}, ApiMembership={HasApi}",
                email, profile.HasWebAccount, profile.HasApiMembership);

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building cross-system user profile for email: {Email}", email);
            return new CrossSystemUserProfileDto { Email = email };
        }
    }

    /// <summary>
    /// Gets member information from the local API system by email
    /// </summary>
    /// <param name="email">Email address to search for</param>
    /// <returns>Member information if found, null otherwise</returns>
    private Task<MemberDto?> GetApiMemberByEmailAsync(string email)
    {
        try
        {
            // Use the existing member service to find by email
            var members = _memberService.GetAllMembers();
            var member = members.FirstOrDefault(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (member == null)
            {
                _logger.LogInformation("No member found in API system for email: {Email}", email);
                return Task.FromResult<MemberDto?>(null);
            }

            _logger.LogInformation("Found member in API system for email: {Email}", email);

            // Convert to DTO
            var dto = new MemberDto
            {
                Id = member.Id,
                Name = member.Name,
                Email = member.Email,
                CreatedAt = member.CreatedAt,
                IsActive = member.IsActive
            };

            return Task.FromResult<MemberDto?>(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API member for email: {Email}", email);
            return Task.FromResult<MemberDto?>(null);
        }
    }
}