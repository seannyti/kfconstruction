using KfConstructionAPI.Models;

namespace KfConstructionAPI.Services.Interfaces;

/// <summary>
/// Interface defining operations for managing member data
/// </summary>
public interface IMemberService
{
    /// <summary>
    /// Retrieves all members from the database
    /// </summary>
    /// <returns>A collection of all members</returns>
    IEnumerable<Member> GetAllMembers();

    /// <summary>
    /// Retrieves a member by their unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the member</param>
    /// <returns>The member if found, null otherwise</returns>
    Member? GetMemberById(int id);

    /// <summary>
    /// Retrieves a member by their associated Identity user ID
    /// </summary>
    /// <param name="userId">The Identity user ID associated with the member</param>
    /// <returns>The member if found, null otherwise</returns>
    Member? GetMemberByUserId(string userId);

    /// <summary>
    /// Adds a new member to the database
    /// </summary>
    /// <param name="member">The member to add</param>
    void AddMember(Member member);

    /// <summary>
    /// Updates an existing member in the database
    /// </summary>
    /// <param name="id">The unique identifier of the member to update</param>
    /// <param name="member">The updated member data</param>
    /// <returns>True if the member was updated successfully, false if not found</returns>
    bool UpdateMember(int id, Member member);

    /// <summary>
    /// Deletes a member from the database
    /// </summary>
    /// <param name="id">The unique identifier of the member to delete</param>
    /// <returns>True if the member was deleted successfully, false if not found</returns>
    bool DeleteMember(int id);
}
