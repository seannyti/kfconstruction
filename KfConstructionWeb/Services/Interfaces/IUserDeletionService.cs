using KfConstructionWeb.Models.DTOs;

namespace KfConstructionWeb.Services.Interfaces;

public interface IUserDeletionService
{
    Task<bool> DeleteUserCompletelyAsync(string userId);
    Task<MemberDto?> GetMemberByUserIdAsync(string userId);
}
