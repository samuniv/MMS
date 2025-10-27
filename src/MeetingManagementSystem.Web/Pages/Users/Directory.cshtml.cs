using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Web.Pages.Users;

[Authorize]
public class DirectoryModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DirectoryModel> _logger;

    public DirectoryModel(UserManager<User> userManager, ILogger<DirectoryModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public List<User> Users { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var query = _userManager.Users.Where(u => u.IsActive);
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(u => 
                    u.FirstName.Contains(SearchTerm) ||
                    u.LastName.Contains(SearchTerm) ||
                    u.Department.Contains(SearchTerm) ||
                    u.Position.Contains(SearchTerm) ||
                    u.Email.Contains(SearchTerm));
            }
            
            Users = await query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            
            _logger.LogInformation("Retrieved {Count} users for directory", Users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user directory");
            TempData["ErrorMessage"] = "An error occurred while loading the user directory.";
        }
    }
}
