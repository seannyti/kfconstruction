using KfConstructionAPI.Models;
using KfConstructionAPI.Models.DTOs;
using KfConstructionAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;
using Asp.Versioning;

namespace KfConstructionAPI.Controllers;

/// <summary>
/// API controller for managing member data and operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly ICrossSystemService _crossSystemService;
    private readonly ILogger<MembersController> _logger;

    /// <summary>
    /// Initializes a new instance of the MembersController
    /// </summary>
    /// <param name="memberService">The member service for data operations</param>
    /// <param name="crossSystemService">The cross-system service for user linking</param>
    /// <param name="logger">The logger instance for tracking operations</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
    public MembersController(
        IMemberService memberService, 
        ICrossSystemService crossSystemService,
        ILogger<MembersController> logger)
    {
        _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
        _crossSystemService = crossSystemService ?? throw new ArgumentNullException(nameof(crossSystemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Helper Methods

    /// <summary>
    /// Maps a Member entity to a MemberDto for API responses
    /// </summary>
    /// <param name="member">The member entity to map</param>
    /// <returns>A populated MemberDto</returns>
    private static MemberDto MapToDto(Member member)
    {
        if (member == null) throw new ArgumentNullException(nameof(member));

        return new MemberDto
        {
            Id = member.Id,
            Name = member.Name,
            Email = member.Email,
            CreatedAt = member.CreatedAt,
            IsActive = member.IsActive
        };
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    /// <param name="result">The data to include in the response</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>A standardized API response</returns>
    private static APIResponse CreateSuccessResponse(object? result = null, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new APIResponse
        {
            Result = result,
            StatusCode = statusCode,
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    /// <param name="errorMessage">The error message to include</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>A standardized error response</returns>
    private static APIResponse CreateErrorResponse(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            errorMessage = "An error occurred processing your request.";

        return new APIResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessages = { errorMessage }
        };
    }

    /// <summary>
    /// Creates a validation error response from ModelState
    /// </summary>
    /// <param name="modelState">The ModelState containing validation errors</param>
    /// <returns>A standardized validation error response</returns>
    private static APIResponse CreateValidationErrorResponse(ModelStateDictionary modelState)
    {
        var errors = modelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(msg => !string.IsNullOrWhiteSpace(msg))
            .ToList();

        if (!errors.Any())
            errors.Add("One or more validation errors occurred.");

        return new APIResponse
        {
            IsSuccess = false,
            StatusCode = HttpStatusCode.BadRequest,
            ErrorMessages = errors
        };
    }

    #endregion

    #region API Endpoints


    /// <summary>
    /// Retrieves all members from the system
    /// </summary>
    /// <returns>A standardized API response containing all members</returns>
    /// <response code="200">Returns all members successfully</response>
    /// <response code="500">If an error occurred retrieving members</response>
    [HttpGet]
    [ProducesResponseType(typeof(APIResponse), 200)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public ActionResult<APIResponse> GetAllMembers()
    {
        try
        {
            _logger.LogInformation("Retrieving all members");
            var members = _memberService.GetAllMembers();
            var dtos = members.Select(MapToDto).ToList();
            var response = CreateSuccessResponse(dtos);
            
            _logger.LogInformation("Retrieved {Count} members successfully", dtos.Count);
            return StatusCode((int)response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all members");
            var response = CreateErrorResponse("Error retrieving members. Please try again later.");
            return StatusCode((int)response.StatusCode, response);
        }
    }


    /// <summary>
    /// Retrieves a specific member by their unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the member to retrieve</param>
    /// <returns>A standardized API response containing the requested member</returns>
    /// <response code="200">Returns the member successfully</response>
    /// <response code="404">If the member is not found</response>
    /// <response code="500">If an error occurred retrieving the member</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(APIResponse), 200)]
    [ProducesResponseType(typeof(APIResponse), 404)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public ActionResult<APIResponse> GetMember(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving member with ID: {MemberId}", id);
            var member = _memberService.GetMemberById(id);
            if (member == null)
            {
                _logger.LogWarning("Member with ID {MemberId} not found", id);
                var errorResponse = CreateErrorResponse($"Member with id {id} not found.", HttpStatusCode.NotFound);
                return StatusCode((int)errorResponse.StatusCode, errorResponse);
            }

            var dto = MapToDto(member);
            var response = CreateSuccessResponse(dto);
            _logger.LogInformation("Member with ID {MemberId} retrieved successfully", id);
            return StatusCode((int)response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving member with ID: {MemberId}", id);
            var response = CreateErrorResponse("Error retrieving member. Please try again later.");
            return StatusCode((int)response.StatusCode, response);
        }
    }

    /// <summary>
    /// Retrieves a member by their associated user ID
    /// </summary>
    /// <param name="userId">The Identity user ID associated with the member</param>
    /// <returns>A standardized API response containing the requested member</returns>
    /// <response code="200">Returns the member successfully</response>
    /// <response code="404">If the member is not found</response>
    /// <response code="500">If an error occurred retrieving the member</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(APIResponse), 200)]
    [ProducesResponseType(typeof(APIResponse), 404)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public ActionResult<APIResponse> GetMemberByUserId(string userId)
    {
        try
        {
            _logger.LogInformation("Retrieving member with User ID: {UserId}", userId);
            var member = _memberService.GetMemberByUserId(userId);
            if (member == null)
            {
                _logger.LogWarning("Member with User ID {UserId} not found", userId);
                var errorResponse = CreateErrorResponse($"Member with user ID {userId} not found.", HttpStatusCode.NotFound);
                return StatusCode((int)errorResponse.StatusCode, errorResponse);
            }

            var dto = MapToDto(member);
            var response = CreateSuccessResponse(dto);
            _logger.LogInformation("Member with User ID {UserId} retrieved successfully", userId);
            return StatusCode((int)response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving member with User ID: {UserId}", userId);
            var response = CreateErrorResponse("Error retrieving member. Please try again later.");
            return StatusCode((int)response.StatusCode, response);
        }
    }

    /// <summary>
    /// Creates a new member in the system
    /// </summary>
    /// <param name="dto">The member data transfer object containing member details</param>
    /// <returns>A standardized API response containing the created member</returns>
    /// <response code="201">Returns the newly created member</response>
    /// <response code="400">If the member data is invalid</response>
    /// <response code="500">If an error occurred creating the member</response>
    [HttpPost]
    [ProducesResponseType(typeof(APIResponse), 201)]
    [ProducesResponseType(typeof(APIResponse), 400)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public ActionResult<APIResponse> CreateMember([FromBody] CreateMemberDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for member creation: {@ModelState}", ModelState.ToDictionary(ms => ms.Key, ms => ms.Value?.Errors?.Select(e => e.ErrorMessage)));
            var validationResponse = CreateValidationErrorResponse(ModelState);
            return StatusCode((int)validationResponse.StatusCode, validationResponse);
        }

        try
        {
            _logger.LogInformation("Creating new member with email: {Email}", dto.Email);
            var member = new Member
            {
                Name = dto.Name,
                Email = dto.Email,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _memberService.AddMember(member);
            
            _logger.LogInformation("Member created successfully with ID: {MemberId}", member.Id);
            var response = CreateSuccessResponse(member, HttpStatusCode.Created);
            return StatusCode((int)response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating member with email: {Email}", dto.Email);
            var response = CreateErrorResponse("Error creating member. Please try again later.");
            return StatusCode((int)response.StatusCode, response);
        }
    }


    /// <summary>
    /// Creates a new member with optional automatic linking to existing web account
    /// </summary>
    /// <param name="dto">The member data transfer object containing member details</param>
    /// <returns>A standardized API response containing the created member and cross-system profile</returns>
    /// <response code="201">Returns the newly created member with cross-system information</response>
    /// <response code="400">If the member data is invalid</response>
    /// <response code="409">If member already exists</response>
    /// <response code="500">If an error occurred creating the member</response>
    [HttpPost("smart-register")]
    [ProducesResponseType(typeof(APIResponse), 201)]
    [ProducesResponseType(typeof(APIResponse), 400)]
    [ProducesResponseType(typeof(APIResponse), 409)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public async Task<ActionResult<APIResponse>> SmartRegisterMember([FromBody] CreateMemberDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for smart member registration: {@ModelState}", ModelState.ToDictionary(ms => ms.Key, ms => ms.Value?.Errors?.Select(e => e.ErrorMessage)));
            var validationResponse = CreateValidationErrorResponse(ModelState);
            return StatusCode((int)validationResponse.StatusCode, validationResponse);
        }

        try
        {
            _logger.LogInformation("Smart registering new member with email: {Email}", dto.Email);

            // Check if member already exists in API
            var existingMembers = _memberService.GetAllMembers();
            var existingMember = existingMembers.FirstOrDefault(m => m.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase));
            
            if (existingMember != null)
            {
                _logger.LogWarning("Member already exists with email: {Email}", dto.Email);
                var conflictResponse = CreateErrorResponse($"Member with email {dto.Email} already exists.", HttpStatusCode.Conflict);
                return StatusCode((int)conflictResponse.StatusCode, conflictResponse);
            }

            // Check if user exists in web system
            var webClient = await _crossSystemService.GetWebClientByEmailAsync(dto.Email);
            
            if (webClient != null)
            {
                _logger.LogInformation("Found existing web client for email: {Email}, creating linked API member", dto.Email);
                // Use web client data to enhance member creation
                dto.Name = string.IsNullOrEmpty(dto.Name) ? webClient.FullName : dto.Name;
            }
            else
            {
                _logger.LogInformation("No existing web client found for email: {Email}, creating standalone API member", dto.Email);
            }

            // Create the member
            var member = new Member
            {
                Name = dto.Name,
                Email = dto.Email,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            
            _memberService.AddMember(member);

            // Get the complete cross-system profile
            var crossSystemProfile = await _crossSystemService.GetCrossSystemUserProfileAsync(dto.Email);

            var result = new
            {
                Member = MapToDto(member),
                CrossSystemProfile = crossSystemProfile,
                Message = webClient != null 
                    ? "Member created and linked to existing web account" 
                    : "Member created as standalone account"
            };
            
            _logger.LogInformation("Smart registration completed for email: {Email}. WebAccount: {HasWeb}, ApiMember: {HasApi}", 
                dto.Email, crossSystemProfile.HasWebAccount, crossSystemProfile.HasApiMembership);
            
            var response = CreateSuccessResponse(result, HttpStatusCode.Created);
            return StatusCode((int)response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during smart registration for email: {Email}", dto.Email);
            var response = CreateErrorResponse("Error creating member. Please try again later.");
            return StatusCode((int)response.StatusCode, response);
        }
    }


    /// <summary>
    /// Checks if a user already exists across systems before registration
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>Information about existing accounts</returns>
    /// <response code="200">Returns account status information</response>
    /// <response code="400">If email is invalid</response>
    /// <response code="500">If an error occurred</response>
    [HttpGet("check-existing/{email}")]
    [ProducesResponseType(typeof(APIResponse), 200)]
    [ProducesResponseType(typeof(APIResponse), 400)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public async Task<ActionResult<APIResponse>> CheckExistingUser(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("CheckExistingUser called with null or empty email");
            var errorResponse = CreateErrorResponse("Email address is required.", HttpStatusCode.BadRequest);
            return StatusCode((int)errorResponse.StatusCode, errorResponse);
        }

        try
        {
            _logger.LogInformation("Checking existing user status for email: {Email}", email);
            
            var profile = await _crossSystemService.GetCrossSystemUserProfileAsync(email);
            
            var result = new
            {
                Email = email,
                ExistsInWebSystem = profile.HasWebAccount,
                ExistsInApiSystem = profile.HasApiMembership,
                WebAccountDetails = profile.WebClient,
                ApiMemberDetails = profile.ApiMember,
                RecommendedAction = GetRecommendedAction(profile),
                Message = GetStatusMessage(profile)
            };

            var response = CreateSuccessResponse(result);
            _logger.LogInformation("User status check completed for: {Email}", email);
            return StatusCode((int)response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existing user for email: {Email}", email);
            var response = CreateErrorResponse("Error checking user status. Please try again later.");
            return StatusCode((int)response.StatusCode, response);
        }
    }

    /// <summary>
    /// Gets recommended action based on user's existing accounts
    /// </summary>
    private static string GetRecommendedAction(CrossSystemUserProfileDto profile)
    {
        return profile switch
        {
            { HasWebAccount: true, HasApiMembership: true } => "LOGIN_BOTH_SYSTEMS",
            { HasWebAccount: true, HasApiMembership: false } => "CREATE_API_MEMBERSHIP",
            { HasWebAccount: false, HasApiMembership: true } => "CREATE_WEB_ACCOUNT",
            _ => "REGISTER_NEW_USER"
        };
    }

    /// <summary>
    /// Gets status message based on user's existing accounts
    /// </summary>
    private static string GetStatusMessage(CrossSystemUserProfileDto profile)
    {
        return profile switch
        {
            { HasWebAccount: true, HasApiMembership: true } => "User already exists in both systems",
            { HasWebAccount: true, HasApiMembership: false } => "User exists in web system, can create linked API membership",
            { HasWebAccount: false, HasApiMembership: true } => "User exists in API system, can create linked web account",
            _ => "New user - can register in either system"
        };
    }


    /// <summary>
    /// Updates an existing member's information</summary>
    /// </summary>
    /// <param name="id">The unique identifier of the member to update</param>
    /// <param name="dto">The updated member data</param>
    /// <returns>A status indicating success or failure of the update operation</returns>
    /// <response code="204">Member updated successfully (no content returned)</response>
    /// <response code="400">If the member data is invalid or ID mismatch</response>
    /// <response code="404">If the member is not found</response>
    /// <response code="500">If an error occurred updating the member</response>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(APIResponse), 400)]
    [ProducesResponseType(typeof(APIResponse), 404)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public ActionResult UpdateMember(int id, [FromBody] UpdateMemberDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for member update: {@ModelState}", ModelState.ToDictionary(ms => ms.Key, ms => ms.Value?.Errors?.Select(e => e.ErrorMessage)));
            var validationResponse = CreateValidationErrorResponse(ModelState);
            return StatusCode((int)validationResponse.StatusCode, validationResponse);
        }

        if (id != dto.Id)
        {
            _logger.LogWarning("ID mismatch in member update - URL ID: {UrlId}, Body ID: {BodyId}", id, dto.Id);
            var errorResponse = CreateErrorResponse("ID in URL does not match ID in body.", HttpStatusCode.BadRequest);
            return StatusCode((int)errorResponse.StatusCode, errorResponse);
        }

        try
        {
            _logger.LogInformation("Updating member with ID: {MemberId}", id);
            var existingMember = _memberService.GetMemberById(id);
            if (existingMember == null)
            {
                _logger.LogWarning("Member with ID {MemberId} not found for update", id);
                var notFoundResponse = CreateErrorResponse($"Member with id {id} not found.", HttpStatusCode.NotFound);
                return StatusCode((int)notFoundResponse.StatusCode, notFoundResponse);
            }

            existingMember.Name = dto.Name;
            existingMember.Email = dto.Email;
            existingMember.IsActive = dto.IsActive;

            var result = _memberService.UpdateMember(id, existingMember);
            if (!result)
            {
                _logger.LogError("Failed to update member with ID: {MemberId}", id);
                var errorResponse = CreateErrorResponse($"Failed to update member with id {id}.");
                return StatusCode((int)errorResponse.StatusCode, errorResponse);
            }

            _logger.LogInformation("Member with ID {MemberId} updated successfully", id);
            return NoContent(); // 204 with no body
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member with ID: {MemberId}", id);
            var response = CreateErrorResponse("Error updating member. Please try again later.");
            return StatusCode((int)response.StatusCode, response);
        }
    }


    /// <summary>
    /// Deletes a member from the system
    /// </summary>
    /// <param name="id">The unique identifier of the member to delete</param>
    /// <returns>A status indicating success or failure of the delete operation</returns>
    /// <response code="204">Member deleted successfully (no content returned)</response>
    /// <response code="404">If the member is not found</response>
    /// <response code="500">If an error occurred deleting the member</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(APIResponse), 404)]
    [ProducesResponseType(typeof(APIResponse), 500)]
    public ActionResult DeleteMember(int id)
    {
        try
        {
            _logger.LogInformation("Deleting member with ID: {MemberId}", id);
            var result = _memberService.DeleteMember(id);
            if (!result)
            {
                _logger.LogWarning("Member with ID {MemberId} not found for deletion", id);
                var errorResponse = CreateErrorResponse($"Member with id {id} not found.", HttpStatusCode.NotFound);
                return StatusCode((int)errorResponse.StatusCode, errorResponse);
            }

            _logger.LogInformation("Member with ID {MemberId} deleted successfully", id);
            return NoContent(); // 204 with no body
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting member with ID: {MemberId}", id);
            var response = CreateErrorResponse("Error deleting member. Please try again later.");
            return StatusCode((int)response.StatusCode, response);
        }
    }

    #endregion
}
