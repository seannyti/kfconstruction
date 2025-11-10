using KfConstructionAPI.Models.DTOs;

namespace KfConstructionAPI.Services.Interfaces;

/// <summary>
/// Interface for cross-system communication to link users by email
/// </summary>
public interface ICrossSystemService
{
    /// <summary>
    /// Gets client information from the web application by email
    /// </summary>
    /// <param name="email">Email address to search for</param>
    /// <returns>Client information if found, null otherwise</returns>
    Task<WebClientDto?> GetWebClientByEmailAsync(string email);

    /// <summary>
    /// Gets complete user profile across all systems by email
    /// </summary>
    /// <param name="email">Email address to search for</param>
    /// <returns>Aggregated user profile data</returns>
    Task<CrossSystemUserProfileDto> GetCrossSystemUserProfileAsync(string email);
}