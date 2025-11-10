using KfConstructionAPI.Models;
using KfConstructionAPI.Data;
using KfConstructionAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionAPI.Services;

/// <summary>
/// Service class for managing member operations
/// </summary>
public class MemberService : IMemberService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MemberService> _logger;

    /// <summary>
    /// Initializes a new instance of the MemberService
    /// </summary>
    /// <param name="context">The database context for member operations</param>
    /// <param name="logger">The logger instance for tracking operations</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
    public MemberService(ApplicationDbContext context, ILogger<MemberService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all members from the database
    /// </summary>
    /// <returns>A collection of all members</returns>
    public IEnumerable<Member> GetAllMembers()
    {
        try
        {
            _logger.LogInformation("Retrieving all members from database");
            var members = _context.Members.ToList();
            _logger.LogInformation("Retrieved {Count} members from database", members.Count);
            return members;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all members");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a member by their unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the member</param>
    /// <returns>The member if found, null otherwise</returns>
    public Member? GetMemberById(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving member with ID: {MemberId}", id);
            var member = _context.Members.Find(id);
            if (member != null)
            {
                _logger.LogInformation("Member with ID {MemberId} found", id);
            }
            else
            {
                _logger.LogWarning("Member with ID {MemberId} not found", id);
            }
            return member;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving member with ID: {MemberId}", id);
            throw;
        }
    }

    /// <summary>
    /// Adds a new member to the database
    /// </summary>
    /// <param name="member">The member to add</param>
    /// <exception cref="ArgumentNullException">Thrown when member is null</exception>
    public void AddMember(Member member)
    {
        if (member == null)
        {
            _logger.LogError("Attempted to add null member");
            throw new ArgumentNullException(nameof(member));
        }

        try
        {
            _logger.LogInformation("Adding new member with email: {Email}", member.Email);
            _context.Members.Add(member);
            _context.SaveChanges();
            _logger.LogInformation("Member added successfully with ID: {MemberId}", member.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding member with email: {Email}", member.Email);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing member in the database
    /// </summary>
    /// <param name="id">The unique identifier of the member to update</param>
    /// <param name="member">The updated member data</param>
    /// <returns>True if the member was updated successfully, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when member is null</exception>
    public bool UpdateMember(int id, Member member)
    {
        if (member == null)
        {
            _logger.LogError("Attempted to update member with null data for ID: {MemberId}", id);
            throw new ArgumentNullException(nameof(member));
        }

        try
        {
            _logger.LogInformation("Updating member with ID: {MemberId}", id);
            var existingMember = _context.Members.Find(id);
            if (existingMember == null)
            {
                _logger.LogWarning("Member with ID {MemberId} not found for update", id);
                return false;
            }

            existingMember.Name = member.Name;
            existingMember.Email = member.Email;
            existingMember.IsActive = member.IsActive;

            _context.SaveChanges();
            _logger.LogInformation("Member with ID {MemberId} updated successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating member with ID: {MemberId}", id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a member from the database
    /// </summary>
    /// <param name="id">The unique identifier of the member to delete</param>
    /// <returns>True if the member was deleted successfully, false if not found</returns>
    public bool DeleteMember(int id)
    {
        try
        {
            _logger.LogInformation("Deleting member with ID: {MemberId}", id);
            var member = _context.Members.Find(id);
            if (member == null)
            {
                _logger.LogWarning("Member with ID {MemberId} not found for deletion", id);
                return false;
            }

            _context.Members.Remove(member);
            _context.SaveChanges();
            _logger.LogInformation("Member with ID {MemberId} deleted successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting member with ID: {MemberId}", id);
            throw;
        }
    }
}
