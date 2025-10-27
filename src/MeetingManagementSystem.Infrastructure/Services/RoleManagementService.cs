using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Infrastructure.Services;

public class RoleManagementService : IRoleManagementService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public RoleManagementService(
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<string>> GetAllRolesAsync()
    {
        return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Enumerable.Empty<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> AssignRoleToUserAsync(int userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return false;
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> IsInRoleAsync(int userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, roleName);
    }

    public async Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return Enumerable.Empty<User>();
        }

        return await _userManager.GetUsersInRoleAsync(roleName);
    }
}
