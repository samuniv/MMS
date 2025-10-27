using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IRoleManagementService
{
    Task<IEnumerable<string>> GetAllRolesAsync();
    Task<IEnumerable<string>> GetUserRolesAsync(int userId);
    Task<bool> AssignRoleToUserAsync(int userId, string roleName);
    Task<bool> RemoveRoleFromUserAsync(int userId, string roleName);
    Task<bool> IsInRoleAsync(int userId, string roleName);
    Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName);
}
