using KfConstructionAPI.Services.Interfaces;
using KfConstructionAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Asp.Versioning;

namespace KfConstructionAPI.Controllers;

/// <summary>
/// API controller for cross-system user operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class UserProfileController : ControllerBase
{
    private readonly ICrossSystemService _crossSystemService;
    private readonly ILogger<UserProfileController> _logger;

    public UserProfileController(
        ICrossSystemService crossSystemService,
        ILogger<UserProfileController> logger)
    {
        _crossSystemService = crossSystemService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a complete user profile across all systems by email
    /// </summary>
    /// <param name="email">Email address to search for</param>
    /// <returns>Aggregated user profile from web app and API systems</returns>
    /// <response code="200">Returns the complete user profile</response>
    /// <response code="404">If no user is found in any system</response>
    /// <response code="400">If email is invalid</response>
    /// <response code="500">If an error occurred</response>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(typeof(APIResponse), 200)]
    [ProducesResponseType(typeof(APIResponse), 404)]
    [ProducesResponseType(typeof(APIResponse), 400)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public async Task<ActionResult<APIResponse>> GetUserProfileByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("GetUserProfileByEmail called with null or empty email");
            var errorResponse = new APIResponse
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessages = { "Email address is required" }
            };
            return BadRequest(errorResponse);
        }

        try
        {
            _logger.LogInformation("Retrieving cross-system user profile for email: {Email}", email);
            
            var profile = await _crossSystemService.GetCrossSystemUserProfileAsync(email);
            
            // Check if user exists in any system
            if (!profile.HasWebAccount && !profile.HasApiMembership)
            {
                _logger.LogInformation("No user found in any system for email: {Email}", email);
                var notFoundResponse = new APIResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessages = { $"No user found with email: {email}" }
                };
                return NotFound(notFoundResponse);
            }

            var response = new APIResponse
            {
                Result = profile,
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true
            };

            _logger.LogInformation(
                "Retrieved cross-system profile for {Email}: WebAccount={HasWeb}, ApiMembership={HasApi}",
                email, profile.HasWebAccount, profile.HasApiMembership);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cross-system user profile for email: {Email}", email);
            var response = new APIResponse
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.InternalServerError,
                ErrorMessages = { "Error retrieving user profile. Please try again later." }
            };
            return StatusCode(500, response);
        }
    }

    /// <summary>
    /// Gets web application client data by email
    /// </summary>
    /// <param name="email">Email address to search for</param>
    /// <returns>Client data from the web application</returns>
    /// <response code="200">Returns the client data</response>
    /// <response code="404">If no client is found</response>
    /// <response code="400">If email is invalid</response>
    /// <response code="500">If an error occurred</response>
    [HttpGet("web-client/{email}")]
    [ProducesResponseType(typeof(APIResponse), 200)]
    [ProducesResponseType(typeof(APIResponse), 404)]
    [ProducesResponseType(typeof(APIResponse), 400)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public async Task<ActionResult<APIResponse>> GetWebClientByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("GetWebClientByEmail called with null or empty email");
            var errorResponse = new APIResponse
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessages = { "Email address is required" }
            };
            return BadRequest(errorResponse);
        }

        try
        {
            _logger.LogInformation("Retrieving web client for email: {Email}", email);
            
            var client = await _crossSystemService.GetWebClientByEmailAsync(email);
            
            if (client == null)
            {
                _logger.LogInformation("No web client found for email: {Email}", email);
                var notFoundResponse = new APIResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessages = { $"No web client found with email: {email}" }
                };
                return NotFound(notFoundResponse);
            }

            var response = new APIResponse
            {
                Result = client,
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true
            };

            _logger.LogInformation("Retrieved web client for email: {Email}", email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving web client for email: {Email}", email);
            var response = new APIResponse
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.InternalServerError,
                ErrorMessages = { "Error retrieving client data. Please try again later." }
            };
            return StatusCode(500, response);
        }
    }
}