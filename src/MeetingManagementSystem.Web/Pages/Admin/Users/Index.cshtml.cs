using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Core.Constants;

namespace MeetingManagementSystem.Web.Pages.Admin.Users;

[Authorize(Policy = Policies.UserManagement)]
public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IRoleManagementService _roleManagementService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        UserManager<User> userManager,
        IRoleManagementService roleManagementService,
        ILogger<IndexModel> logger)
    {
        _userManager = userManager;
        _roleManagementService = roleManagementService;
        _logger = logger;
    }

    public List<User> Users { get; set; } = new();
    public Dictionary<int, List<string>> UserRoles { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        var query = _userManager.Users.AsQueryable();
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query = query.Where(u => 
                u.FirstName.Contains(SearchTerm) ||
                u.LastName.Contains(SearchTerm) ||
                u.Email.Contains(SearchTerm) ||
                u.Department.Contains(SearchTerm));
        }
        
        // Apply status filter
        if (!string.IsNullOrWhiteSpace(StatusFilter))
        {
            var isActive = StatusFilter.ToLower() == "active";
            query = query.Where(u => u.IsActive == isActive);
        }
        
        Users = await query.OrderBy(u => u.LastName).ToListAsync();

        foreach (var user in Users)
        {
            var roles = await _roleManagementService.GetUserRolesAsync(user.Id);
            UserRoles[user.Id] = roles.ToList();
        }
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage();
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} status changed to {Status}", user.Id, user.IsActive ? "Active" : "Inactive");
            TempData["SuccessMessage"] = $"User {user.Email} has been {(user.IsActive ? "activated" : "deactivated")}.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update user status.";
        }

        return RedirectToPage();
    }
}
