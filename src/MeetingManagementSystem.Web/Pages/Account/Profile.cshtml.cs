using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IRoleManagementService _roleManagementService;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(
        UserManager<User> userManager,
        IRoleManagementService roleManagementService,
        ILogger<ProfileModel> logger)
    {
        _userManager = userManager;
        _roleManagementService = roleManagementService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    
    [BindProperty]
    public PasswordInputModel PasswordInput { get; set; } = new();
    
    public List<string> UserRoles { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [StringLength(200)]
        public string OfficeLocation { get; set; } = string.Empty;
    }

    public class PasswordInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadUserDataAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await LoadUserRolesAsync(user);
            }
            return Page();
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            currentUser.FirstName = Input.FirstName;
            currentUser.LastName = Input.LastName;
            currentUser.Department = Input.Department;
            currentUser.Position = Input.Position;
            currentUser.OfficeLocation = Input.OfficeLocation;

            var result = await _userManager.UpdateAsync(currentUser);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} updated their profile", currentUser.Id);
                TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            ModelState.AddModelError(string.Empty, "An error occurred while updating your profile.");
        }

        await LoadUserRolesAsync(currentUser);
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            await LoadUserDataAsync(user);
            return Page();
        }

        try
        {
            var result = await _userManager.ChangePasswordAsync(
                user,
                PasswordInput.CurrentPassword,
                PasswordInput.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} changed their password", user.Id);
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            ModelState.AddModelError(string.Empty, "An error occurred while changing your password.");
        }

        await LoadUserDataAsync(user);
        return Page();
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(userId.ToString());
    }

    private async Task LoadUserDataAsync(User user)
    {
        Input = new InputModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Department = user.Department,
            Position = user.Position,
            OfficeLocation = user.OfficeLocation
        };

        CreatedAt = user.CreatedAt;
        await LoadUserRolesAsync(user);
    }

    private async Task LoadUserRolesAsync(User user)
    {
        var roles = await _roleManagementService.GetUserRolesAsync(user.Id);
        UserRoles = roles.ToList();
    }
}
