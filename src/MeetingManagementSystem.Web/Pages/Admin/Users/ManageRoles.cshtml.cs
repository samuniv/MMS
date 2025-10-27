using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Core.Constants;

namespace MeetingManagementSystem.Web.Pages.Admin.Users;

[Authorize(Policy = Policies.UserManagement)]
public class ManageRolesModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IRoleManagementService _roleManagementService;
    private readonly ILogger<ManageRolesModel> _logger;

    public ManageRolesModel(
        UserManager<User> userManager,
        IRoleManagementService roleManagementService,
        ILogger<ManageRolesModel> logger)
    {
        _userManager = userManager;
        _roleManagementService = roleManagementService;
        _logger = logger;
    }

    public User? User { get; set; }
    public List<string> CurrentRoles { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        User = await _userManager.FindByIdAsync(id.ToString());
        if (User == null)
        {
            return NotFound();
        }

        await LoadRolesAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignRoleAsync(int userId, string roleName)
    {
        var success = await _roleManagementService.AssignRoleToUserAsync(userId, roleName);
        
        if (success)
        {
            _logger.LogInformation("Role {RoleName} assigned to user {UserId}", roleName, userId);
            TempData["SuccessMessage"] = $"Role '{roleName}' has been assigned successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Failed to assign role '{roleName}'.";
        }

        return RedirectToPage(new { id = userId });
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(int userId, string roleName)
    {
        var success = await _roleManagementService.RemoveRoleFromUserAsync(userId, roleName);
        
        if (success)
        {
            _logger.LogInformation("Role {RoleName} removed from user {UserId}", roleName, userId);
            TempData["SuccessMessage"] = $"Role '{roleName}' has been removed successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Failed to remove role '{roleName}'.";
        }

        return RedirectToPage(new { id = userId });
    }

    private async Task LoadRolesAsync(int userId)
    {
        var allRoles = await _roleManagementService.GetAllRolesAsync();
        CurrentRoles = (await _roleManagementService.GetUserRolesAsync(userId)).ToList();
        AvailableRoles = allRoles.Except(CurrentRoles).ToList();
    }
}
